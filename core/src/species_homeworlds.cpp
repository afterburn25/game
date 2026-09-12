#include <stellar/core/species_environment.hpp>

#include <algorithm>
#include <bit>
#include <cmath>
#include <cstdint>
#include <limits>
#include <stdexcept>
#include <unordered_map>
#include <unordered_set>

namespace stellar::core {
namespace {
constexpr std::string_view terran = "terran_baseline";
constexpr std::string_view pelagic = "pelagic_high_pressure";
constexpr std::string_view compact = "compact_high_gravity";
constexpr std::string_view cryogenic = "cryogenic_hydrocarbon";

std::uint64_t mix(std::uint64_t value) {
    value += 0x9E3779B97F4A7C15ULL;
    value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9ULL;
    value = (value ^ (value >> 27)) * 0x94D049BB133111EBULL;
    return value ^ (value >> 31);
}

double within_system_score(const PlanetSpeciesAssessment& assessment) {
    return assessment.environment.natural_habitability * 10.0 +
        (assessment.suitability == SettlementSuitability::Comfortable ? .35 : 0.0);
}

struct Candidate { const StellarSystem* system; const PlanetaryBody* body; PlanetSpeciesAssessment assessment; };
struct CandidateSet { int civilization_id; std::string species_id; std::vector<Candidate> candidates; };

std::vector<Candidate> build_candidates(const SpeciesEnvironmentProfile& species, std::span<const PlanetaryBody> bodies,
    const std::unordered_map<int, const StellarSystem*>& systems_by_id, bool canonical_sol) {
    std::vector<Candidate> result;
    for (const auto& body : bodies) {
        if (body.has_pre_warp_civilization) continue;
        if (canonical_sol && (species.id == terran ? !(body.system_id == sol_system_id && body.id == earth_body_id) : body.system_id == sol_system_id)) continue;
        const auto assessment = assess_species_planet(species, body);
        if (!assessment.naturally_colonizable) continue;
        const auto found = systems_by_id.find(body.system_id);
        if (found == systems_by_id.end()) throw std::invalid_argument{"Planetary body references an unknown system."};
        result.push_back({found->second, &body, assessment});
    }
    return result;
}

double score_candidate(const Candidate& candidate, const std::vector<SpeciesHomeworldAssignment>& chosen,
    const std::unordered_map<int, const StellarSystem*>& systems_by_id) {
    double spread = 0.0;
    if (!chosen.empty()) {
        spread = std::numeric_limits<double>::infinity();
        for (const auto& existing : chosen) {
            const auto system = systems_by_id.find(existing.system_id);
            if (system == systems_by_id.end()) throw std::invalid_argument{"Chosen homeworld references an unknown system."};
            spread = std::min(spread, squared_distance_light_years(candidate.system->position, system->second->position));
        }
    }
    return within_system_score(candidate.assessment) + std::min(1.0, std::sqrt(spread) / 500.0);
}
} // namespace

std::string assign_species(std::int64_t campaign_seed, int civilization_id, bool canonical_start) {
    if (civilization_id < 0) throw std::out_of_range{"Civilization IDs must be non-negative."};
    const auto seed = std::bit_cast<std::uint64_t>(campaign_seed) ^ static_cast<std::uint64_t>(static_cast<std::uint32_t>(civilization_id)) * 0xD1B54A32D192ED03ULL;
    const auto value = mix(seed);
    if (!canonical_start) {
        const auto profiles = species_environment_profiles();
        if (profiles.empty()) throw std::invalid_argument{"No species definitions are available."};
        return profiles[value % profiles.size()].id;
    }
    if (civilization_id == 0) return std::string{terran};
    switch (value % 3) {
    case 0: return std::string{pelagic};
    case 1: return std::string{compact};
    default: return std::string{cryogenic};
    }
}

std::vector<SpeciesHomeworldAssignment> plan_species_homeworlds(std::span<const StellarSystem> systems,
    std::span<const PlanetaryBody> bodies, std::span<const std::string> species_ids) {
    if (species_ids.empty()) return {};
    if (systems.size() < species_ids.size()) throw std::invalid_argument{"There are fewer star systems than founding civilizations."};
    std::unordered_map<int, const StellarSystem*> systems_by_id;
    systems_by_id.reserve(systems.size());
    bool canonical_sol = false;
    for (const auto& system : systems) {
        if (!systems_by_id.emplace(system.id, &system).second) throw std::invalid_argument{"Duplicate star system ID."};
        canonical_sol = canonical_sol || (system.catalog_preset_id && *system.catalog_preset_id == sol_catalog_preset_id);
    }

    std::vector<CandidateSet> sets;
    sets.reserve(species_ids.size());
    for (std::size_t index = 0; index < species_ids.size(); ++index) {
        const auto& species = species_environment_profile(species_ids[index]);
        sets.push_back({static_cast<int>(index), species.id, build_candidates(species, bodies, systems_by_id, canonical_sol)});
    }
    std::string missing;
    for (const auto& set : sets) if (set.candidates.empty()) {
        if (!missing.empty()) missing += ", ";
        missing += "civ " + std::to_string(set.civilization_id) + " / " + set.species_id;
    }
    if (!missing.empty()) throw std::invalid_argument{"Natural homeworld planning failed because no compatible uninhabited body exists for: " + missing + "."};

    std::stable_sort(sets.begin(), sets.end(), [](const CandidateSet& left, const CandidateSet& right) {
        std::unordered_set<int> left_systems, right_systems;
        for (const auto& item : left.candidates) left_systems.insert(item.system->id);
        for (const auto& item : right.candidates) right_systems.insert(item.system->id);
        return left_systems.size() != right_systems.size() ? left_systems.size() < right_systems.size() : left.civilization_id < right.civilization_id;
    });
    std::vector<SpeciesHomeworldAssignment> chosen;
    chosen.reserve(species_ids.size());
    std::unordered_set<int> occupied;
    for (const auto& set : sets) {
        const Candidate* best = nullptr;
        double best_score = 0.0;
        for (const auto& candidate : set.candidates) {
            if (occupied.contains(candidate.system->id)) continue;
            const auto score = score_candidate(candidate, chosen, systems_by_id);
            if (!best || score > best_score || (score == best_score && (candidate.assessment.environment.natural_habitability > best->assessment.environment.natural_habitability ||
                (candidate.assessment.environment.natural_habitability == best->assessment.environment.natural_habitability &&
                 (candidate.system->id < best->system->id || (candidate.system->id == best->system->id && candidate.body->id < best->body->id)))))) {
                best = &candidate; best_score = score;
            }
        }
        if (!best) {
            std::unordered_set<int> available_set;
            for (const auto& candidate : set.candidates) available_set.insert(candidate.system->id);
            std::vector<int> available{available_set.begin(), available_set.end()}; std::sort(available.begin(), available.end());
            std::string ids;
            for (const auto id : available) { if (!ids.empty()) ids += ","; ids += std::to_string(id); }
            throw std::invalid_argument{"Natural homeworld planning exhausted distinct systems for civ " + std::to_string(set.civilization_id) +
                " / " + set.species_id + ". Compatible systems: [" + ids + "]."};
        }
        occupied.insert(best->system->id);
        chosen.push_back({set.civilization_id, set.species_id, best->system->id, best->body->id,
            best->assessment.environment.natural_habitability, best->assessment.suitability});
    }
    std::sort(chosen.begin(), chosen.end(), [](const auto& left, const auto& right) { return left.civilization_id < right.civilization_id; });
    return chosen;
}
} // namespace stellar::core
