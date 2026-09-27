#pragma once
#include <chrono>
#include <cstdint>

namespace nc::regionalphysical {
inline constexpr uint32_t RegionalPreparationVertexBudget=65536u;
inline constexpr uint32_t RegionalDemandVertexBudget=32768u;
// The target, dependency mask and prepared pupil belong to one publication.
// Kept independent of Vulkan so the production handoff can be exercised offline.
template<class Frame, class Job, class Demand, class Complete>
bool ResolveDependencyTarget(Frame& target, Job& job, Demand& demand, uint32_t slot,
    uint64_t generation, uint64_t topology, bool allResident,
    uint32_t preparedPupil, Complete complete) {
  // Completion belongs to job.frame, not to the newest camera request. Keep
  // that frame through staged preparation and its publication fence. Releasing
  // at dependency completion can restart demand on every moving-camera update
  // forever, leaving the old physical pupil as the render owner.
  const bool dependencyComplete=job.phase==2&&complete(job.mask);
  const bool awaitingPublication=slot!=0||job.frame.identity[0]!=preparedPupil;
  if(!allResident&&job.generation==generation&&job.topology==topology&&job.frame.metadata[0]&&
     (!dependencyComplete||awaitingPublication))target=job.frame;
  if(!allResident&&(job.frame.identity[0]!=target.identity[0]||job.generation!=generation||job.topology!=topology)){
    job={};job.frame=target;job.generation=generation;job.topology=topology;job.started=std::chrono::steady_clock::now();
    demand.frames[slot]=target;demand.masks[slot].fill(0);
  }
  return allResident||(job.phase==2&&complete(job.mask));
}
}
