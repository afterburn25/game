#include "galaxy_main.hpp"
#include "stellar/build_version.hpp"
#include <stellar/engine/runtime_paths.hpp>
#include <stellar/core/galaxy_catalog.hpp>
#include <stellar/core/planetary_catalog.hpp>
#include <stellar/core/species_environment.hpp>
#include <stellar/core/civilization_catalog.hpp>
#include <nlohmann/json.hpp>
#include <charconv>
#include <chrono>
#include <fstream>
#include <iostream>
#include <stdexcept>
using namespace stellar::core;
using Json=nlohmann::json;
namespace {
std::int64_t signed_number(const std::string& text) {
    std::int64_t n{}; const auto parsed=std::from_chars(text.data(),text.data()+text.size(),n);
    if(parsed.ec!=std::errc{} || parsed.ptr!=text.data()+text.size()) throw std::invalid_argument("Expected a signed 64-bit numeric seed/count");
    return n;
}
Json system_json(const StellarSystem& s) {
    return {{"id",s.id},{"name",s.name},{"xLightYears",s.position.x},{"yLightYears",s.position.y},
        {"depthLightYears",s.position.depth_light_years},{"primaryClass",s.primary},{"secondaryClass",s.secondary},
        {"tertiaryClass",s.tertiary},{"catalogPresetId",s.catalog_preset_id},{"stellarCatalogId",s.stellar_catalog_id},
        {"archetype",s.archetype},{"hasHabitableWorld",s.has_habitable_world},{"hasAnomaly",s.has_anomaly},
        {"hasRareResource",s.has_rare_resource},{"hasPreWarpCivilization",s.has_pre_warp_civilization}};
}
Json body_json(const PlanetaryBody& b) {
    const auto& e=b.environment;
    return {{"id",b.id},{"systemId",b.system_id},{"parentBodyId",b.parent_body_id},{"orbitIndex",b.orbit_index},
        {"name",b.name},{"kind",b.kind},{"radiusEarth",b.radius_earth},{"massEarth",b.mass_earth},
        {"legacyColonizationCandidate",b.legacy_colonization_candidate},{"hasRareResource",b.has_rare_resource},
        {"hasAnomaly",b.has_anomaly},{"hasPreWarpCivilization",b.has_pre_warp_civilization},
        {"orbitalEccentricity",b.orbital_eccentricity},{"orbitalInclinationDegrees",b.orbital_inclination_degrees},
        {"environment",{{"gravityG",e.gravity_g},{"temperatureKelvin",e.temperature_kelvin},{"pressureKPa",e.pressure_kpa},
            {"atmosphere",e.atmosphere},{"availableSolvent",e.available_solvent},{"radiationHazard",e.radiation_hazard},
            {"isImmersedEnvironment",e.is_immersed_environment},{"hasSolidSurface",e.has_solid_surface}}}};
}
Json civilization_json(const Civilization& c) {
    const auto& t=c.traits; Json offices=Json::array();
    for(const auto& o:c.leadership) offices.push_back({{"office",o.office},{"characterId",o.character.id},
        {"displayName",o.character.display_name},{"voiceProfileId",o.character.voice_profile_id},{"portrait",o.character.portrait}});
    return {{"id",c.id},{"name",c.name},{"homeSystemId",c.home_system_id},{"speciesId",c.species_id},
        {"archetype",c.archetype},{"isPlayer",c.is_player},{"developmentStage",c.development_stage},
        {"isSeededAncient",c.is_seeded_ancient},{"expansionAllowed",c.expansion_allowed},
        {"neutralUnlessProvoked",c.neutral_unless_provoked},{"leadership",std::move(offices)},
        {"traits",{{"aggression",t.aggression},{"territoriality",t.territoriality},{"greed",t.greed},
            {"scientificCuriosity",t.scientific_curiosity},{"riskTolerance",t.risk_tolerance},
            {"survivalPriority",t.survival_priority},{"honorBound",t.honor_bound}}}};
}
}
int run_galaxy_catalog(int argc,char** argv) {
    std::int64_t seed=8374837,count=500,repeats=1;
    std::int64_t pre_warp_count=6,ancient_count=1;
    bool plan_homes=false,found_civilizations=false,founding_options=false,constrained_fallback=false;
    std::string player_species="terran_baseline";
    auto asset_root=stellar::engine::executable_directory(); std::filesystem::path output;
    for(int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if(arg=="--generate-galaxy" || arg=="--headless") continue;
        if(arg=="--plan-homes") { plan_homes=true; continue; }
        if(arg=="--found-civilizations") { found_civilizations=true; continue; }
        if(i+1==argc) throw std::invalid_argument("Missing value for "+arg);
        const std::string value=argv[++i];
        if(arg=="--seed") seed=signed_number(value);
        else if(arg=="--systems") count=signed_number(value);
        else if(arg=="--repeat") repeats=signed_number(value);
        else if(arg=="--asset-root") asset_root=value;
        else if(arg=="--catalog-output") output=value;
        else if(arg=="--civilizations") { pre_warp_count=signed_number(value); founding_options=true; }
        else if(arg=="--ancients") { ancient_count=signed_number(value); founding_options=true; }
        else if(arg=="--player-species") { player_species=value; founding_options=true; }
        else throw std::invalid_argument("Unknown galaxy argument: "+arg);
    }
    if((count!=250 && count!=500 && count!=1000 && count!=2500) || repeats<1 || repeats>100)
        throw std::invalid_argument("Galaxy size must be 250, 500, 1000 or 2500; repeat must be 1..100");
    if(pre_warp_count<1||pre_warp_count>13||ancient_count<0||ancient_count>3)
        throw std::invalid_argument("Ordinary civilizations must be 1..13 and ancient civilizations 0..3");
    if(founding_options&&!found_civilizations) throw std::invalid_argument("Civilization options require --found-civilizations");
    if(plan_homes&&found_civilizations) throw std::invalid_argument("Choose --plan-homes or --found-civilizations; they are different migration stages");
    if(found_civilizations) (void)species_environment_profile(player_species);
    const auto input=std::filesystem::absolute(asset_root/"Data/astronomy/hyg-nearby-500-v1.json");
    const auto catalog=load_nearby_catalog(input);
    const auto start=std::chrono::steady_clock::now(); std::vector<StellarSystem> systems;
    std::vector<PlanetaryBody> bodies;
    std::vector<SpeciesHomeworldAssignment> homes;
    std::vector<Civilization> civilizations;
    for(std::int64_t i=0;i<repeats;++i) {
        systems=generate_stellar_catalog(seed,static_cast<int>(count),catalog);
        if(found_civilizations) {
            auto founded=create_founding_catalog(seed,systems,static_cast<int>(pre_warp_count),static_cast<int>(ancient_count),player_species);
            systems=std::move(founded.systems); bodies=std::move(founded.bodies); civilizations=std::move(founded.civilizations);
            constrained_fallback=founded.used_constrained_home_fallback;
            homes.clear();
            for(const auto& c:civilizations) homes.push_back(resolve_species_homeworld(c.id,c.species_id,c.home_system_id,bodies));
        } else bodies=generate_planetary_catalog(seed,systems);
        if(plan_homes) {
            std::vector<std::string> species;
            // The existing default full-galaxy profile seeds six ordinary factions and one ancient.
            for(int civilization=0;civilization<7;++civilization) species.push_back(assign_species(seed,civilization,true));
            homes=plan_species_homeworlds(systems,bodies,species);
        }
    }
    const double elapsed=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
    Json records=Json::array(); for(const auto& system:systems) records.push_back(system_json(system));
    Json planets=Json::array(),sol=Json::array();
    for(const auto& body:bodies) {
        planets.push_back(body_json(body));
        if(body.system_id==0) sol.push_back(body_json(body));
    }
    const auto core=full_galaxy_core(static_cast<int>(count));
    Json home_records=Json::array();
    for(const auto& home:homes) home_records.push_back({{"civilizationId",home.civilization_id},{"speciesId",home.species_id},
        {"systemId",home.system_id},{"planetaryBodyId",home.planetary_body_id},{"naturalHabitability",home.natural_habitability},
        {"suitability",home.suitability}});
    Json civilization_records=Json::array(); for(const auto& c:civilizations) civilization_records.push_back(civilization_json(c));
    Json snapshot={{"format",found_civilizations?"stellar-founding-catalog-v1":"stellar-physical-catalog-v1"},
        {"phase",found_civilizations?"founding-before-colonies":"physical-before-civilizations"},
        {"seed",seed},{"count",count},{"generatorVersion","full-galaxy-compact-v1"},
        {"settingsProfile","existing-full-galaxy-defaults"},
        {"homeworldPlanning",found_civilizations?"with-nearby-expansion":plan_homes?"normal-before-nearby-expansion":"not-requested"},
        {"usedConstrainedHomeFallback",constrained_fallback},{"civilizations",std::move(civilization_records)},
        {"homeworldPreview",std::move(home_records)},
        {"radiusLightYears",full_galaxy_radius(static_cast<int>(count))},
        {"core",{{"x",core.position.x},{"y",core.position.y},{"exclusionRadius",core.exclusion_radius}}},
        {"systems",std::move(records)},{"planetaryBodies",std::move(planets)},{"solBodies",std::move(sol)}};
    if(!output.empty()) {
        auto pending=output; pending+=".pending";
        if(std::filesystem::exists(output) || std::filesystem::exists(pending)) throw std::runtime_error("Refusing to overwrite catalog output: "+output.string());
        { std::ofstream stream(pending); if(!stream) throw std::runtime_error("Cannot create catalog output: "+pending.string());
          stream<<snapshot.dump()<<'\n'; stream.flush(); if(!stream) throw std::runtime_error("Catalog output write failed: "+pending.string()); }
        std::filesystem::rename(pending,output);
    }
    std::cout<<Json({{"mode",found_civilizations?"founding-catalog":"physical-stellar-catalog"},{"engineVersion",STELLAR_ENGINE_VERSION},
        {"gameplayParity",false},{"sourceCommit",STELLAR_SOURCE_COMMIT},{"seed",seed},{"systems",count},
        {"measuredSystems",96},{"solBodies",snapshot.at("solBodies").size()},{"planetaryBodies",bodies.size()},{"repeats",repeats},
        {"normalHomeworldPlanning",plan_homes},{"plannedHomeworlds",homes.size()},
        {"foundingCivilizations",civilizations.size()},{"usedConstrainedHomeFallback",constrained_fallback},
        {"elapsedMs",elapsed},{"meanGenerationMs",elapsed/static_cast<double>(repeats)},
        {"assetPath",input.string()},{"catalogOutput",output.string()}}).dump()<<'\n';
    return 0;
}
