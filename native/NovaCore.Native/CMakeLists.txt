cmake_minimum_required(VERSION 3.24)
project(NovaCoreNative LANGUAGES CXX)

find_package(Vulkan REQUIRED)
find_program(GLSLC_EXECUTABLE glslc REQUIRED HINTS "$ENV{VULKAN_SDK}/Bin")

set(SHADER_DIR "${CMAKE_CURRENT_SOURCE_DIR}/shaders")
set(SPIRV_DIR "${CMAKE_CURRENT_BINARY_DIR}/shaders")
add_custom_command(OUTPUT "${SPIRV_DIR}/triangle.vert.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/triangle.vert" -o "${SPIRV_DIR}/triangle.vert.spv"
  DEPENDS "${SHADER_DIR}/triangle.vert")
add_custom_command(OUTPUT "${SPIRV_DIR}/triangle.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/triangle.frag" -o "${SPIRV_DIR}/triangle.frag.spv"
  DEPENDS "${SHADER_DIR}/triangle.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/orbit.vert.spv" "${SPIRV_DIR}/orbit.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/orbit.vert" -o "${SPIRV_DIR}/orbit.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/orbit.frag" -o "${SPIRV_DIR}/orbit.frag.spv"
  DEPENDS "${SHADER_DIR}/orbit.vert" "${SHADER_DIR}/orbit.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/orbit_previous.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/orbit_previous.frag" -o "${SPIRV_DIR}/orbit_previous.frag.spv"
  DEPENDS "${SHADER_DIR}/orbit_previous.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/body_forward.frag.spv" "${SPIRV_DIR}/sas_target.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/body_forward.frag" -o "${SPIRV_DIR}/body_forward.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/sas_target.frag" -o "${SPIRV_DIR}/sas_target.frag.spv"
  DEPENDS "${SHADER_DIR}/body_forward.frag" "${SHADER_DIR}/sas_target.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary.vert.spv" "${SPIRV_DIR}/planetary.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/planetary.vert" -o "${SPIRV_DIR}/planetary.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/planetary.frag" -o "${SPIRV_DIR}/planetary.frag.spv"
  DEPENDS "${SHADER_DIR}/planetary.vert" "${SHADER_DIR}/planetary.frag")
add_custom_target(NovaCoreShaders DEPENDS "${SPIRV_DIR}/triangle.vert.spv" "${SPIRV_DIR}/triangle.frag.spv" "${SPIRV_DIR}/orbit.vert.spv" "${SPIRV_DIR}/orbit.frag.spv" "${SPIRV_DIR}/orbit_previous.frag.spv" "${SPIRV_DIR}/body_forward.frag.spv" "${SPIRV_DIR}/sas_target.frag.spv" "${SPIRV_DIR}/planetary.vert.spv" "${SPIRV_DIR}/planetary.frag.spv")

add_library(NovaCore.Native SHARED NovaCoreNative.cpp NovaCoreNative.h)
add_dependencies(NovaCore.Native NovaCoreShaders)
target_compile_features(NovaCore.Native PRIVATE cxx_std_20)
target_compile_definitions(NovaCore.Native PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX VK_USE_PLATFORM_WIN32_KHR NC_NATIVE_EXPORTS $<$<CONFIG:Debug>:NC_DEBUG_BUILD=1>)
target_link_libraries(NovaCore.Native PRIVATE Vulkan::Vulkan user32)
add_custom_command(TARGET NovaCore.Native POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different "$<TARGET_FILE:NovaCore.Native>" "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/NovaCore.Native.dll")
