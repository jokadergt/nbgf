#include "profiles/bc1bf755618240e8.h"
#include <cstdint>
#include <iostream>
namespace profile = cesi_verified_bc1bf755618240e8;
// Keep pointer addition distinct from a pointer read in this profile.
static_assert(std::uintptr_t(0x100000) + profile::ObservedLuaApp::PrimaryToInstance == 0x1001F0);
static_assert(profile::ChildrenVector::Stride == 2 * sizeof(std::uint64_t));
static_assert(profile::TaskScheduler::JobStride == 2 * sizeof(std::uint64_t));
static_assert(profile::Instance::Name == 0x68 && profile::Instance::Name != 0xB0);
static_assert(profile::MsvcString::Length == 16);
static_assert(profile::MsvcString::Capacity == 24);
static_assert(profile::MsvcString::InlineCapacity == 15);
static_assert(profile::CapabilitiesStorage::EntryKey + sizeof(std::uint64_t) == profile::CapabilitiesStorage::EntryStride);
static_assert(profile::ModuleScript::ModuleScriptByteCode == 0x120);
int main() { std::cout << "Versioned profile layout checks passed (offline only)\n"; }
