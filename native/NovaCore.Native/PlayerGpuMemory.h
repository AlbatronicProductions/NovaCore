#pragma once
#include <cstdint>

// One process-scoped, adapter-pinned driver estimate. Capacity is separate from
// budget. Every heap is reported once; only DEVICE_LOCAL heaps contribute to totals.
struct alignas(8) NcPlayerGpuMemorySnapshot {
  uint32_t size, version, status, heapCount;
  uint64_t sequence, qpc, frequency, utcFileTime, queryNanoseconds;
  uint64_t usageBytes, budgetBytes, capacityBytes;
  uint32_t vendorId, deviceId, driverVersion, localHeapMask;
  uint8_t uuid[16], luid[8];
  uint32_t luidValid, reserved;
  char adapterName[256], detail[160];
  uint64_t heapUsage[16], heapBudget[16], heapCapacity[16];
  uint32_t heapFlags[16];
};
static_assert(sizeof(NcPlayerGpuMemorySnapshot)==992);

