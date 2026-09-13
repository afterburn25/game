#pragma once

#include <stellar/core/exploration_planning.hpp>
#include <stellar/core/industry_allocation.hpp>

#include <optional>
#include <span>
#include <string>
#include <vector>

namespace stellar::core {

enum class ExplorationEventType {
  SystemDetected,
  SystemReconnoitered,
  SystemSurveyStarted,
  SystemSurveyed,
  ResourceSignatureDetected,
  AnomalySignatureDetected,
  ActivitySignatureDetected,
  ResourceSurveyed,
  AnomalySurveyed,
  NativeCivilizationSurveyed,
  SensorContact,
  FirstContact,
};

struct ExplorationEvent {
  ExplorationEventType type{};
  int civilization_id{};
  int fleet_id{};
  int system_id{};
  std::string message;
  std::optional<int> target_civilization_id;
  std::optional<int> planetary_body_id;
};

struct ExplorationAdvanceWorldView {
  std::span<const StellarSystem> systems;
  std::span<const PlanetaryBody> bodies;
  std::span<const Civilization> civilizations;
  std::span<FleetState> fleets;
  std::span<const Colony> colonies;
  std::span<const CivilizationEconomy> economies;
  CivilizationKnowledgeState &knowledge;
  InterstellarLaneNetwork &lanes;
};

class ExplorationSimulation {
public:
  static constexpr double science_survey_progress_per_day = 0.08;
  static constexpr double scout_reconnaissance_progress = 0.35;
  static constexpr double scout_reconnaissance_days = 2.0;

  explicit ExplorationSimulation(
      ExplorationReachAssessment operational_reach = {});

  [[nodiscard]] MissionReachAssessment
  assess_operational_reach(ExplorationPlanningWorldView world, int fleet_id,
                           int destination_system_id) const;

  std::vector<ExplorationEvent>
  advance(ExplorationAdvanceWorldView world, double simulation_delta) const;

private:
  ExplorationMissionPlanner mission_planner_;
  SurveyOperationsProfiler survey_profiler_;
};

} // namespace stellar::core
