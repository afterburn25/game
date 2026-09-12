#include "galaxy_main.hpp"
#include "stellar/build_version.hpp"
#include <stellar/engine/runtime_paths.hpp>
#include <stellar/core/galaxy_catalog.hpp>
#include <stellar/core/planetary_catalog.hpp>
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
}
int run_galaxy_catalog(int argc,char** argv) {
    std::int64_t seed=8374837,count=500,repeats=1;
    auto asset_root=stellar::engine::executable_directory(); std::filesystem::path output;
    for(int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if(arg=="--generate-galaxy" || arg=="--headless") continue;
        if(i+1==argc) throw std::invalid_argument("Missing value for "+arg);
        const std::string value=argv[++i];
        if(arg=="--seed") seed=signed_number(value);
        else if(arg=="--systems") count=signed_number(value);
        else if(arg=="--repeat") repeats=signed_number(value);
        else if(arg=="--asset-root") asset_root=value;
        else if(arg=="--catalog-output") output=value;
        else throw std::invalid_argument("Unknown galaxy argument: "+arg);
    }
    if((count!=250 && count!=500 && count!=1000 && count!=2500) || repeats<1 || repeats>100)
        throw std::invalid_argument("Galaxy size must be 250, 500, 1000 or 2500; repeat must be 1..100");
    const auto input=std::filesystem::absolute(asset_root/"Data/astronomy/hyg-nearby-500-v1.json");
    const auto catalog=load_nearby_catalog(input);
    const auto start=std::chrono::steady_clock::now(); std::vector<StellarSystem> systems;
    std::vector<PlanetaryBody> bodies;
    for(std::int64_t i=0;i<repeats;++i) {
        systems=generate_stellar_catalog(seed,static_cast<int>(count),catalog);
        bodies=generate_planetary_catalog(seed,systems);
    }
    const double elapsed=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
    Json records=Json::array(); for(const auto& system:systems) records.push_back(system_json(system));
    Json planets=Json::array(),sol=Json::array();
    for(const auto& body:bodies) {
        planets.push_back(body_json(body));
        if(body.system_id==0) sol.push_back(body_json(body));
    }
    const auto core=full_galaxy_core(static_cast<int>(count));
    Json snapshot={{"format","stellar-physical-catalog-v1"},{"phase","physical-before-civilizations"},
        {"seed",seed},{"count",count},{"generatorVersion","full-galaxy-compact-v1"},
        {"settingsProfile","existing-full-galaxy-defaults"},
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
    std::cout<<Json({{"mode","physical-stellar-catalog"},{"engineVersion",STELLAR_ENGINE_VERSION},
        {"gameplayParity",false},{"sourceCommit",STELLAR_SOURCE_COMMIT},{"seed",seed},{"systems",count},
        {"measuredSystems",96},{"solBodies",snapshot.at("solBodies").size()},{"planetaryBodies",bodies.size()},{"repeats",repeats},
        {"elapsedMs",elapsed},{"meanGenerationMs",elapsed/static_cast<double>(repeats)},
        {"assetPath",input.string()},{"catalogOutput",output.string()}}).dump()<<'\n';
    return 0;
}
