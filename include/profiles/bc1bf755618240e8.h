#pragma once
#include <cstdint>

// User-supplied read-only observations for version-bc1bf755618240e8.
// Checked for consistency with the supplied report, not independently in a client.
// Partial structural offsets; this is not a complete executor offset set.
// LuaApp was inspected. In-experience execution and bypass are unverified.
namespace cesi_verified_bc1bf755618240e8 {
namespace Instance {
inline constexpr std::uintptr_t ClassDescriptor = 0x18;
inline constexpr std::uintptr_t Name = 0x68;
inline constexpr std::uintptr_t Parent = 0x60;
inline constexpr std::uintptr_t Children = 0x70;
}
namespace ClassDescriptor {
inline constexpr std::uintptr_t Name = 0x8;
}
namespace ChildrenVector {
inline constexpr std::uintptr_t Begin = 0x0;
inline constexpr std::uintptr_t End = 0x8;
inline constexpr std::uintptr_t Stride = 0x10;
}
namespace MsvcString {
inline constexpr std::uintptr_t Length = 0x10;
inline constexpr std::uintptr_t Capacity = 0x18;
inline constexpr std::uintptr_t InlineCapacity = 15;
}
namespace DataModel {
inline constexpr std::uintptr_t Workspace = 0x148;
}
namespace ObservedLuaApp {
// Reread and validate class/name before using. Runtime addresses are not RVAs.
inline constexpr std::uintptr_t GlobalPrimaryA = 0x8EB67A0;
inline constexpr std::uintptr_t GlobalPrimaryB = 0x8EB7D80;
// Instance = ReadPointer(moduleBase + GlobalPrimaryA) + PrimaryToInstance;
inline constexpr std::uintptr_t PrimaryToInstance = 0x1F0;
}
namespace Pointer {
inline constexpr std::uintptr_t VisualEnginePointer = 0x8942CB8;
inline constexpr std::uintptr_t TaskScheduler = 0x8E4D5F8;
}
namespace TaskScheduler {
inline constexpr std::uintptr_t JobStart = 0xC8;
inline constexpr std::uintptr_t JobEnd = 0xD0;
inline constexpr std::uintptr_t JobStride = 0x10;
}
namespace Jobs {
inline constexpr std::uintptr_t JobName = 0x18;
}
namespace ModuleScript {
// Points directly to the SharedString holding bytecode.
inline constexpr std::uintptr_t ModuleScriptByteCode = 0x120;
}
namespace SharedString {
inline constexpr std::uintptr_t Data = 0x10;
inline constexpr std::uintptr_t Size = 0x20;
inline constexpr std::uintptr_t Capacity = 0x28;
inline constexpr std::uintptr_t RefCount = 0x38;
}
namespace RenderJob {
inline constexpr std::uintptr_t RenderView = 0x1E0;
}
namespace RenderView {
inline constexpr std::uintptr_t VisualEngine = 0x20;
}
namespace CapabilitiesStorage {
// Capabilities is obtained by a property-key lookup, not Instance + 0x208.
inline constexpr std::uintptr_t InstancePropertyStore = 0x38;
inline constexpr std::uintptr_t PropertyKeyRva = 0x8E32D34;
inline constexpr std::uintptr_t Begin = 0x0;
inline constexpr std::uintptr_t End = 0x8;
inline constexpr std::uintptr_t Overflow = 0x18;
inline constexpr std::uintptr_t EntryStride = 0x10;
inline constexpr std::uintptr_t EntryValue = 0x0;
inline constexpr std::uintptr_t EntryKey = 0x8;
}
}
