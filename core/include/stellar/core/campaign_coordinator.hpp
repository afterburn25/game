#pragma once

#include <stellar/core/campaign_economy.hpp>
#include <stellar/core/colonization_runtime.hpp>
#include <stellar/core/combat_command_runtime.hpp>
#include <stellar/core/construction_projects.hpp>
#include <stellar/core/exploration_advance.hpp>
#include <stellar/core/freight.hpp>
#include <stellar/core/fresh_campaign.hpp>
#include <stellar/core/legacy_research.hpp>
#include <stellar/core/shipbuilding.hpp>
#include <stellar/core/strategic_runtime.hpp>

#include <cstddef>
#include <functional>
#include <memory>
#include <optional>
#include <span>
#include <string_view>
#include <variant>
#include <vector>

namespace stellar::core {

struct CombatCivilizationOutcomeSummary {
  int civilization_id{};
  double shield_damage_dealt{};
  double armor_damage_dealt{};
  double hull_damage_dealt{};
  double shield_damage_taken{};
  double armor_damage_taken{};
  double hull_damage_taken{};
  int enemy_vessels_destroyed{};
  int own_vessels_lost{};
  int retreats_initiated{};
  int successful_escapes{};
  double embarked_population_casualties_inflicted_millions{};
  double embarked_population_casualties_suffered_millions{};

  [[nodiscard]] double total_damage_dealt() const noexcept;
  [[nodiscard]] double total_damage_taken() const noexcept;
};

struct CombatSystemOutcomeSummary {
  std::optional<int> system_id;
  int event_count{};
  int engagements_started{};
  int engagements_ended{};
  int damage_events{};
  int vessels_destroyed{};
  int retreats_initiated{};
  int successful_escapes{};
  double total_damage_applied{};
  double embarked_population_casualties_millions{};
  std::vector<CombatCivilizationOutcomeSummary> civilizations;
};

struct CombatOutcomeSummary {
  int event_count{};
  int engagements_started{};
  int engagements_ended{};
  int damage_events{};
  int vessels_destroyed{};
  int retreats_initiated{};
  int successful_escapes{};
  double total_damage_applied{};
  double embarked_population_casualties_millions{};
  std::vector<CombatCivilizationOutcomeSummary> civilizations;
  std::vector<CombatSystemOutcomeSummary> systems;
};

[[nodiscard]] CombatOutcomeSummary
summarize_combat_outcome(std::span<const CombatEvent> events);

struct SimulationStepResult {
  double simulation_days{};
  std::vector<CivilizationIndustryAllocation> industry_allocations;
  std::vector<ConstructionEvent> construction_events;
  std::vector<ShipbuildingEvent> shipbuilding_events;
  std::vector<LegacyResearchEvent> research_events;
  std::vector<ExplorationEvent> exploration_events;
  std::vector<CombatEvent> combat_events;
  std::vector<ColonizationEvent> colonization_events;

  [[nodiscard]] CombatOutcomeSummary combat_outcome() const;
};

// Owns the mutable fresh-campaign world and the lane graph derived from its
// astronomy. lanes() rebuilds the retained graph if the system geometry changes.
class CampaignSimulationState {
public:
  explicit CampaignSimulationState(FreshCampaignState campaign);

  [[nodiscard]] FreshCampaignState &campaign() noexcept;
  [[nodiscard]] const FreshCampaignState &campaign() const noexcept;
  [[nodiscard]] InterstellarLaneNetwork &lanes();
  [[nodiscard]] std::size_t cached_lane_route_tree_count();

private:
  FreshCampaignState campaign_;
  std::optional<InterstellarLaneNetwork> lanes_;
  std::vector<StellarSystem> lane_astronomy_;
};

using CampaignConstructionCapabilityQuery = std::function<bool(
    std::span<const TechnologyState>, int, std::string_view)>;
using CampaignShipbuildingCapabilityQuery = std::function<bool(
    std::span<const TechnologyState>, int, std::string_view)>;

// The empty queries select the source legacy/prototype capability adapters.
// Adaptive Research must supply its own two matched authority queries.
struct SourceCompatibleCampaignConfiguration {
  bool advance_legacy_research{true};
  bool use_strategic_shipbuilding_preferences{true};
  CampaignConstructionCapabilityQuery construction_capability;
  CampaignShipbuildingCapabilityQuery shipbuilding_capability;
};

// Concrete subsystem instances preserve their own supported dependency
// injection without turning coordinator phases into replaceable callbacks.
struct CampaignSubsystemRuntime {
  LegacyResearchSimulation research{};
  ExplorationSimulation exploration{ExplorationReachAssessment{}};
  FreightSimulation freight{FreightReachAssessor{}};
  ColonizationSimulation colonization{SettlementReachAssessment{}};
};

class GalaxySimulationStepCoordinator {
public:
  explicit GalaxySimulationStepCoordinator(
      SourceCompatibleCampaignConfiguration configuration = {});
  GalaxySimulationStepCoordinator(
      SourceCompatibleCampaignConfiguration configuration,
      CombatCommandRuntime matched_combat,
      CampaignSubsystemRuntime subsystems = {});
  GalaxySimulationStepCoordinator(
      SourceCompatibleCampaignConfiguration configuration,
      CombatSimulation raw_combat,
      CampaignSubsystemRuntime subsystems = {});
  GalaxySimulationStepCoordinator(
      SourceCompatibleCampaignConfiguration configuration,
      CivilizationStrategicRuntimeCoordinator strategic,
      CombatCommandRuntime matched_combat,
      CampaignSubsystemRuntime subsystems = {});
  GalaxySimulationStepCoordinator(
      SourceCompatibleCampaignConfiguration configuration,
      CivilizationStrategicRuntimeCoordinator strategic,
      CombatSimulation raw_combat,
      CampaignSubsystemRuntime subsystems = {});

  [[nodiscard]] SimulationStepResult
  advance(CampaignSimulationState *campaign, double simulation_days);

  [[nodiscard]] bool has_matched_combat_runtime() const noexcept;
  [[nodiscard]] CivilizationStrategicRuntimeCoordinator &strategic_runtime()
      noexcept;
  [[nodiscard]] const CivilizationStrategicRuntimeCoordinator &
  strategic_runtime() const noexcept;
  [[nodiscard]] CombatSimulation &combat_simulation() noexcept;
  [[nodiscard]] const CombatSimulation &combat_simulation() const noexcept;

private:
  bool advance_legacy_research_{};
  bool use_strategic_shipbuilding_preferences_{};
  std::shared_ptr<CampaignConstructionCapabilityQuery>
      construction_capability_;
  std::shared_ptr<CampaignShipbuildingCapabilityQuery>
      shipbuilding_capability_;
  CivilizationStrategicRuntimeCoordinator strategic_;
  std::variant<CombatCommandRuntime, CombatSimulation> combat_;
  CampaignSubsystemRuntime subsystems_;
};

} // namespace stellar::core
