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
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary.vert" -o "${SPIRV_DIR}/planetary.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary.frag" -o "${SPIRV_DIR}/planetary.frag.spv"
  DEPENDS "${SHADER_DIR}/planetary.vert" "${SHADER_DIR}/planetary.frag" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/local_terrain.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_physical_authority.glsl" "${SHADER_DIR}/planet_material.glsl" "${SHADER_DIR}/planet_terrain.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/anchored_terrain.vert.spv" "${SPIRV_DIR}/anchored_terrain.tesc.spv" "${SPIRV_DIR}/anchored_terrain.tese.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/anchored_terrain.vert" -o "${SPIRV_DIR}/anchored_terrain.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/anchored_terrain.tesc" -o "${SPIRV_DIR}/anchored_terrain.tesc.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/anchored_terrain.tese" -o "${SPIRV_DIR}/anchored_terrain.tese.spv"
  DEPENDS "${SHADER_DIR}/anchored_terrain.vert" "${SHADER_DIR}/anchored_terrain.tesc" "${SHADER_DIR}/anchored_terrain.tese" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/local_terrain.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_physical_authority.glsl" "${SHADER_DIR}/anchored_physical_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_select.comp.spv" "${SPIRV_DIR}/planetary_terrain_generate.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_select.comp" -o "${SPIRV_DIR}/planetary_select.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_terrain_generate.comp" -o "${SPIRV_DIR}/planetary_terrain_generate.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_select.comp" "${SHADER_DIR}/planetary_terrain_generate.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/planet_terrain.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_height_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_height_query.comp" -o "${SPIRV_DIR}/planetary_height_query.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_height_query.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_field_query.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_natural_terrain_field_query.comp" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_families_query.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_natural_terrain_families_query.comp" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_prepare.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_natural_terrain_prepare.comp" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_anchored_prepare.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_global_prepare.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_anchored_prepare.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_anchored_prepare.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_natural_terrain_global_prepare.comp" "${SHADER_DIR}/planetary_natural_terrain_anchored_prepare.comp" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl" "${SHADER_DIR}/production_cube_surface.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_mesh_displace.comp.spv" "${SPIRV_DIR}/planetary_mesh_normals.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_mesh_displace.comp" -o "${SPIRV_DIR}/planetary_mesh_displace.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_mesh_normals.comp" -o "${SPIRV_DIR}/planetary_mesh_normals.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_mesh_displace.comp" "${SHADER_DIR}/planetary_mesh_normals.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT
  "${SPIRV_DIR}/spherical_billboard_proof_reset.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_prepare.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_normals.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_cull.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_compact.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof.vert.spv"
  "${SPIRV_DIR}/spherical_billboard_proof.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof_reset.comp" -o "${SPIRV_DIR}/spherical_billboard_proof_reset.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof_prepare.comp" -o "${SPIRV_DIR}/spherical_billboard_proof_prepare.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof_normals.comp" -o "${SPIRV_DIR}/spherical_billboard_proof_normals.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof_cull.comp" -o "${SPIRV_DIR}/spherical_billboard_proof_cull.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof_compact.comp" -o "${SPIRV_DIR}/spherical_billboard_proof_compact.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof.vert" -o "${SPIRV_DIR}/spherical_billboard_proof.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/spherical_billboard_proof.frag" -o "${SPIRV_DIR}/spherical_billboard_proof.frag.spv"
  DEPENDS
    "${SHADER_DIR}/spherical_billboard_proof_reset.comp"
    "${SHADER_DIR}/spherical_billboard_proof_prepare.comp"
    "${SHADER_DIR}/spherical_billboard_proof_normals.comp"
    "${SHADER_DIR}/spherical_billboard_proof_cull.comp"
    "${SHADER_DIR}/spherical_billboard_proof_compact.comp"
    "${SHADER_DIR}/spherical_billboard_proof.vert"
    "${SHADER_DIR}/spherical_billboard_proof.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_production.frag.spv" "${SPIRV_DIR}/planetary_production_terrain.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_production.frag" -o "${SPIRV_DIR}/planetary_production.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_production_terrain.comp" -o "${SPIRV_DIR}/planetary_production_terrain.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_production.frag" "${SHADER_DIR}/planetary_production_terrain.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/local_terrain.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/production_cube_filter.glsl" "${SHADER_DIR}/production_earth_material.glsl" "${SHADER_DIR}/production_terrain_material.glsl" "${SHADER_DIR}/planet_material.glsl" "${SHADER_DIR}/planet_terrain.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/distant_planet.vert.spv" "${SPIRV_DIR}/distant_planet.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/distant_planet.vert" -o "${SPIRV_DIR}/distant_planet.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/distant_planet.frag" -o "${SPIRV_DIR}/distant_planet.frag.spv"
  DEPENDS "${SHADER_DIR}/distant_planet.vert" "${SHADER_DIR}/distant_planet.frag" "${SHADER_DIR}/planet_material.glsl" "${SHADER_DIR}/planet_terrain.glsl" "${SHADER_DIR}/earth_ocean_material.glsl" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/production_cube_filter.glsl" "${SHADER_DIR}/production_earth_material.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_ring.vert.spv" "${SPIRV_DIR}/planetary_ring_far.frag.spv" "${SPIRV_DIR}/planetary_ring_near.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/planetary_ring.vert" -o "${SPIRV_DIR}/planetary_ring.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/planetary_ring_far.frag" -o "${SPIRV_DIR}/planetary_ring_far.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/planetary_ring_near.frag" -o "${SPIRV_DIR}/planetary_ring_near.frag.spv"
  DEPENDS "${SHADER_DIR}/planetary_ring.vert" "${SHADER_DIR}/planetary_ring_far.frag" "${SHADER_DIR}/planetary_ring_near.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/solar_orbit.vert.spv" "${SPIRV_DIR}/solar_orbit.frag.spv" "${SPIRV_DIR}/solar_marker.vert.spv" "${SPIRV_DIR}/solar_marker.frag.spv" "${SPIRV_DIR}/solar_label.vert.spv" "${SPIRV_DIR}/solar_label.frag.spv" "${SPIRV_DIR}/solar_speed_hud.vert.spv" "${SPIRV_DIR}/solar_speed_hud.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_orbit.vert" -o "${SPIRV_DIR}/solar_orbit.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_orbit.frag" -o "${SPIRV_DIR}/solar_orbit.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_marker.vert" -o "${SPIRV_DIR}/solar_marker.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_marker.frag" -o "${SPIRV_DIR}/solar_marker.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_label.vert" -o "${SPIRV_DIR}/solar_label.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/solar_label.frag" -o "${SPIRV_DIR}/solar_label.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/solar_speed_hud.vert" -o "${SPIRV_DIR}/solar_speed_hud.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/solar_speed_hud.frag" -o "${SPIRV_DIR}/solar_speed_hud.frag.spv"
  DEPENDS "${SHADER_DIR}/solar_orbit.vert" "${SHADER_DIR}/solar_orbit.frag" "${SHADER_DIR}/solar_marker.vert" "${SHADER_DIR}/solar_marker.frag" "${SHADER_DIR}/solar_label.vert" "${SHADER_DIR}/solar_label.frag" "${SHADER_DIR}/solar_sans_sdf.glsl" "${SHADER_DIR}/solar_speed_hud.vert" "${SHADER_DIR}/solar_speed_hud.frag")
add_custom_command(OUTPUT "${SPIRV_DIR}/fullscreen.vert.spv" "${SPIRV_DIR}/space_background.frag.spv" "${SPIRV_DIR}/tone_map.frag.spv" "${SPIRV_DIR}/stellar_sun.vert.spv" "${SPIRV_DIR}/stellar_sun.frag.spv" "${SPIRV_DIR}/stellar_glow.vert.spv" "${SPIRV_DIR}/stellar_glow.frag.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/fullscreen.vert" -o "${SPIRV_DIR}/fullscreen.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/space_background.frag" -o "${SPIRV_DIR}/space_background.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/tone_map.frag" -o "${SPIRV_DIR}/tone_map.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/stellar_sun.vert" -o "${SPIRV_DIR}/stellar_sun.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/stellar_sun.frag" -o "${SPIRV_DIR}/stellar_sun.frag.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/stellar_glow.vert" -o "${SPIRV_DIR}/stellar_glow.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} "${SHADER_DIR}/stellar_glow.frag" -o "${SPIRV_DIR}/stellar_glow.frag.spv"
  DEPENDS "${SHADER_DIR}/fullscreen.vert" "${SHADER_DIR}/space_background.frag" "${SHADER_DIR}/tone_map.frag" "${SHADER_DIR}/stellar_sun.vert" "${SHADER_DIR}/stellar_sun.frag" "${SHADER_DIR}/stellar_glow.vert" "${SHADER_DIR}/stellar_glow.frag")
add_custom_target(NovaCoreShaders DEPENDS "${SPIRV_DIR}/triangle.vert.spv" "${SPIRV_DIR}/triangle.frag.spv" "${SPIRV_DIR}/orbit.vert.spv" "${SPIRV_DIR}/orbit.frag.spv" "${SPIRV_DIR}/orbit_previous.frag.spv" "${SPIRV_DIR}/body_forward.frag.spv" "${SPIRV_DIR}/sas_target.frag.spv" "${SPIRV_DIR}/planetary.vert.spv" "${SPIRV_DIR}/planetary.frag.spv" "${SPIRV_DIR}/anchored_terrain.vert.spv" "${SPIRV_DIR}/anchored_terrain.tesc.spv" "${SPIRV_DIR}/anchored_terrain.tese.spv" "${SPIRV_DIR}/planetary_select.comp.spv" "${SPIRV_DIR}/planetary_terrain_generate.comp.spv" "${SPIRV_DIR}/planetary_height_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_anchored_prepare.comp.spv" "${SPIRV_DIR}/distant_planet.vert.spv" "${SPIRV_DIR}/distant_planet.frag.spv" "${SPIRV_DIR}/planetary_ring.vert.spv" "${SPIRV_DIR}/planetary_ring_far.frag.spv" "${SPIRV_DIR}/planetary_ring_near.frag.spv" "${SPIRV_DIR}/solar_orbit.vert.spv" "${SPIRV_DIR}/solar_orbit.frag.spv" "${SPIRV_DIR}/solar_marker.vert.spv" "${SPIRV_DIR}/solar_marker.frag.spv" "${SPIRV_DIR}/solar_label.vert.spv" "${SPIRV_DIR}/solar_label.frag.spv" "${SPIRV_DIR}/solar_speed_hud.vert.spv" "${SPIRV_DIR}/solar_speed_hud.frag.spv" "${SPIRV_DIR}/fullscreen.vert.spv" "${SPIRV_DIR}/space_background.frag.spv" "${SPIRV_DIR}/tone_map.frag.spv" "${SPIRV_DIR}/stellar_sun.vert.spv" "${SPIRV_DIR}/stellar_sun.frag.spv" "${SPIRV_DIR}/stellar_glow.vert.spv" "${SPIRV_DIR}/stellar_glow.frag.spv")

add_custom_target(NovaCoreProductionShaders DEPENDS
  "${SPIRV_DIR}/planetary_production.frag.spv"
  "${SPIRV_DIR}/planetary_production_terrain.comp.spv")
add_custom_target(NovaCoreMeshPreparationShaders DEPENDS
  "${SPIRV_DIR}/planetary_mesh_displace.comp.spv"
  "${SPIRV_DIR}/planetary_mesh_normals.comp.spv")
add_custom_target(NovaCoreSphericalBillboardProofShaders DEPENDS
  "${SPIRV_DIR}/spherical_billboard_proof_reset.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_prepare.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_normals.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_cull.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof_compact.comp.spv"
  "${SPIRV_DIR}/spherical_billboard_proof.vert.spv"
  "${SPIRV_DIR}/spherical_billboard_proof.frag.spv")
add_library(NovaCore.Native SHARED NovaCoreNative.cpp NovaCoreNative.h PlanetaryHeightQuery.cpp PlanetaryHeightQuery.h PlanetaryMeshPreparation.cpp PlanetaryMeshPreparation.h PlanetarySphericalBillboardGpuProof.cpp PlanetarySphericalBillboardGpuProof.h ProductionCubeSurface.cpp ProductionCubeSurface.h LocalTerrainPack.cpp LocalTerrainPack.h)
add_dependencies(NovaCore.Native NovaCoreShaders NovaCoreProductionShaders NovaCoreMeshPreparationShaders NovaCoreSphericalBillboardProofShaders)
target_compile_features(NovaCore.Native PRIVATE cxx_std_20)
target_compile_definitions(NovaCore.Native PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX VK_USE_PLATFORM_WIN32_KHR NC_NATIVE_EXPORTS $<$<CONFIG:Debug>:NC_DEBUG_BUILD=1>)
target_link_libraries(NovaCore.Native PRIVATE Vulkan::Vulkan user32 bcrypt)
add_custom_command(TARGET NovaCore.Native POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different "$<TARGET_FILE:NovaCore.Native>" "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/NovaCore.Native.dll")
