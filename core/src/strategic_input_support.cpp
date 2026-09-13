#include <stellar/core/strategic_input_support.hpp>

#include <algorithm>
#include <cmath>
#include <stdexcept>
#include <vector>

namespace stellar::core {
namespace {
constexpr double epsilon = 0.0000001;

double clamp_finite(double value, double maximum) noexcept {
  return std::clamp(std::isfinite(value) ? value : 0.0, 0.0, maximum);
}

struct FleetReadiness {
  double current_durability{}, maximum_durability{}, repair_deficit{},
      missing_hull{}, current_strength{}, maximum_strength{};
  bool is_armed{}, is_combat_effective{}, is_retreating{}, is_disengaged{};
};

FleetReadiness read_fleet(const FleetState &fleet) {
  const FleetCombatState *state = fleet.combat ? &*fleet.combat : nullptr;
  const CombatProfileDefinition *profile =
      state ? find_combat_profile(state->profile_id) : nullptr;
  const bool persisted = profile != nullptr;
  if (!profile) profile = &get_combat_profile(default_combat_profile_id(fleet.role));

  const double shields = persisted ? clamp_finite(state->shields, profile->max_shields) : profile->max_shields;
  const double armor = persisted ? clamp_finite(state->armor, profile->max_armor) : profile->max_armor;
  const double hull = persisted ? clamp_finite(state->hull, profile->max_hull) : profile->max_hull;
  const double current_durability = shields + armor + hull;
  const double maximum_durability = profile->max_shields + profile->max_armor + profile->max_hull;
  const double missing_hull = std::max(0.0, profile->max_hull - hull);
  const double repair_deficit = std::max(0.0, maximum_durability - current_durability);
  const int order_value = persisted ? static_cast<int>(state->order) : 0;
  const auto order = persisted && order_value >= 0 && order_value <= 3
                         ? state->order
                         : MilitaryOrderType::Hold;
  const bool retreating = order == MilitaryOrderType::Retreat;
  const bool disengaged = persisted && state->is_disengaged && state->disengaged_system_id == fleet.current_system_id;
  const double hull_readiness = profile->max_hull <= epsilon ? 0.0 : std::clamp(hull / profile->max_hull, 0.0, 1.0);
  const double maximum_offense = profile->sustained_damage_per_day() * 3.0;
  const double current_strength = current_durability + maximum_offense * hull_readiness;
  const double maximum_strength = maximum_durability + maximum_offense;
  const bool armed = profile->has_weapon();
  return {current_durability, maximum_durability, repair_deficit, missing_hull,
          current_strength, maximum_strength, armed,
          armed && hull > epsilon && !retreating && !disengaged,
          retreating, disengaged};
}
} // namespace

double CombatReadinessSummary::durability_ratio() const noexcept {
  return maximum_durability <= 0.0 ? 1.0 : std::clamp(current_durability / maximum_durability, 0.0, 1.0);
}

double CombatReadinessSummary::armed_strength_ratio() const noexcept {
  return maximum_armed_strength <= 0.0 ? 1.0 : std::clamp(current_armed_strength / maximum_armed_strength, 0.0, 1.0);
}

CombatReadinessSummary combat_readiness(CombatReadinessView world,
                                        int civilization_id) {
  if (std::none_of(world.civilizations.begin(), world.civilizations.end(),
                   [=](const auto &civilization) { return civilization.id == civilization_id; }))
    throw std::runtime_error("Unknown civilization " + std::to_string(civilization_id) + ".");

  CombatReadinessSummary result;
  result.civilization_id = civilization_id;
  std::vector<const FleetState *> fleets;
  for (const auto &fleet : world.fleets)
    if (fleet.is_active && fleet.civilization_id == civilization_id) fleets.push_back(&fleet);
  std::stable_sort(fleets.begin(), fleets.end(), [](const auto *left, const auto *right) { return left->id < right->id; });

  for (const auto *fleet : fleets) {
    ++result.active_vessels;
    const auto snapshot = read_fleet(*fleet);
    result.current_durability += snapshot.current_durability;
    result.maximum_durability += snapshot.maximum_durability;
    result.total_repair_deficit += snapshot.repair_deficit;
    if (snapshot.repair_deficit > epsilon) ++result.damaged_vessels;
    if (snapshot.missing_hull > epsilon) ++result.hull_damaged_vessels;
    if (snapshot.is_retreating) ++result.retreating_vessels;
    if (snapshot.is_disengaged) ++result.disengaged_vessels;
    if (!snapshot.is_armed) continue;
    ++result.active_armed_vessels;
    result.current_armed_strength += snapshot.current_strength;
    result.maximum_armed_strength += snapshot.maximum_strength;
    if (!snapshot.is_combat_effective) continue;
    ++result.combat_effective_armed_vessels;
    result.combat_effective_armed_strength += snapshot.current_strength;
  }
  return result;
}

bool prototype_construction_has_capability(
    std::span<const TechnologyState> technologies, int civilization_id,
    std::string_view capability_id) noexcept {
  const auto item = std::find_if(technologies.begin(), technologies.end(),
      [=](const auto &technology) { return technology.civilization_id == civilization_id; });
  return item != technologies.end() && item->completed_technology_ids.contains(capability_id);
}

bool prototype_shipbuilding_has_capability(
    std::span<const TechnologyState> technologies, int civilization_id,
    std::string_view capability_id) noexcept {
  const auto item = std::find_if(technologies.begin(), technologies.end(),
      [=](const auto &technology) { return technology.civilization_id == civilization_id; });
  if (item == technologies.end()) return false;
  if (capability_id == "spacecraft_construction")
    return item->completed_technology_ids.contains("orbital_industry");
  if (capability_id == "experimental_interstellar_transit")
    return item->completed_technology_ids.contains("prototype_warp_drive");
  return false;
}
} // namespace stellar::core
