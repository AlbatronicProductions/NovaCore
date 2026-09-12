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
  DEPENDS "${SHADER_DIR}/planetary.vert" "${SHADER_DIR}/planetary.frag" "${SHADER_DIR}/production_cube_surface.glsl"  "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_physical_authority.glsl" "${SHADER_DIR}/planet_material.glsl" "${SHADER_DIR}/planet_terrain.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_select.comp.spv" "${SPIRV_DIR}/planetary_terrain_generate.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_select.comp" -o "${SPIRV_DIR}/planetary_select.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_terrain_generate.comp" -o "${SPIRV_DIR}/planetary_terrain_generate.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_select.comp" "${SHADER_DIR}/planetary_terrain_generate.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/planet_terrain.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_height_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_height_query.comp" -o "${SPIRV_DIR}/planetary_height_query.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_height_query.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_field_query.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv"
  DEPENDS "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_field_query.comp" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_families_query.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv"
  DEPENDS "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_families_query.comp" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_prepare.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv"
  DEPENDS "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_prepare.comp" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_natural_terrain_global_prepare.comp" -o "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv"
  DEPENDS "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_global_prepare.comp"  "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl" "${SHADER_DIR}/production_cube_surface.glsl")
add_custom_command(OUTPUT "${SPIRV_DIR}/planetary_mesh_displace.comp.spv" "${SPIRV_DIR}/planetary_mesh_normals.comp.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_mesh_displace.comp" -o "${SPIRV_DIR}/planetary_mesh_displace.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/planetary_mesh_normals.comp" -o "${SPIRV_DIR}/planetary_mesh_normals.comp.spv"
  DEPENDS "${SHADER_DIR}/planetary_mesh_displace.comp" "${SHADER_DIR}/planetary_mesh_normals.comp" "${SHADER_DIR}/production_cube_surface.glsl" "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/planetary_natural_terrain_surface.glsl" "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/planetary_natural_terrain_families.glsl" "${SHADER_DIR}/planetary_natural_terrain_field.glsl")
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
  DEPENDS "${SHADER_DIR}/facility_light_occlusion.glsl" "${SHADER_DIR}/planetary_production.frag" "${SHADER_DIR}/planetary_production_terrain.comp" "${SHADER_DIR}/production_cube_surface.glsl"  "${SHADER_DIR}/physical_surface.glsl" "${SHADER_DIR}/production_cube_filter.glsl" "${SHADER_DIR}/production_earth_material.glsl" "${SHADER_DIR}/production_terrain_material.glsl" "${SHADER_DIR}/surface_material_coordinates.glsl" "${SHADER_DIR}/planet_material.glsl" "${SHADER_DIR}/planet_terrain.glsl")
add_custom_command(OUTPUT
  "${SPIRV_DIR}/production_spherical_billboard_reset.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_prepare.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_prepare.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_cull.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_compact.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_reset.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_cull.comp.spv"
  "${SPIRV_DIR}/production_nested_scale_mesh_cull.comp.spv"
  "${SPIRV_DIR}/production_nested_scale_mesh_incoming_cull.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_compact.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard.vert.spv"
  "${SPIRV_DIR}/production_spherical_billboard.tesc.spv"
  "${SPIRV_DIR}/production_spherical_billboard.tese.spv"
  COMMAND ${CMAKE_COMMAND} -E make_directory "${SPIRV_DIR}"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_prepare.comp" -o "${SPIRV_DIR}/production_spherical_billboard_prepare.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_incoming_prepare.comp" -o "${SPIRV_DIR}/production_spherical_billboard_incoming_prepare.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_reset.comp" -o "${SPIRV_DIR}/production_spherical_billboard_reset.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_cull.comp" -o "${SPIRV_DIR}/production_spherical_billboard_cull.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_compact.comp" -o "${SPIRV_DIR}/production_spherical_billboard_compact.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_incoming_reset.comp" -o "${SPIRV_DIR}/production_spherical_billboard_incoming_reset.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_incoming_cull.comp" -o "${SPIRV_DIR}/production_spherical_billboard_incoming_cull.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_nested_scale_mesh_cull.comp" -o "${SPIRV_DIR}/production_nested_scale_mesh_cull.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_nested_scale_mesh_incoming_cull.comp" -o "${SPIRV_DIR}/production_nested_scale_mesh_incoming_cull.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard_incoming_compact.comp" -o "${SPIRV_DIR}/production_spherical_billboard_incoming_compact.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard.vert" -o "${SPIRV_DIR}/production_spherical_billboard.vert.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard.tesc" -o "${SPIRV_DIR}/production_spherical_billboard.tesc.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/production_spherical_billboard.tese" -o "${SPIRV_DIR}/production_spherical_billboard.tese.spv"
  DEPENDS "${SHADER_DIR}/facility_support.glsl" "${SHADER_DIR}/regional_physical.glsl" "${SHADER_DIR}/planetary_physical_authority.glsl" "${SHADER_DIR}/production_spherical_billboard_prepare.comp" "${SHADER_DIR}/production_spherical_billboard_incoming_prepare.comp" "${SHADER_DIR}/production_spherical_billboard_physical.glsl" "${SHADER_DIR}/production_spherical_billboard_reset.comp" "${SHADER_DIR}/production_spherical_billboard_cull.comp" "${SHADER_DIR}/production_spherical_billboard_compact.comp" "${SHADER_DIR}/production_spherical_billboard_incoming_reset.comp" "${SHADER_DIR}/production_spherical_billboard_incoming_cull.comp" "${SHADER_DIR}/production_spherical_billboard_incoming_compact.comp" "${SHADER_DIR}/production_nested_scale_mesh_cull.comp" "${SHADER_DIR}/production_nested_scale_mesh_incoming_cull.comp" "${SHADER_DIR}/production_spherical_billboard.vert" "${SHADER_DIR}/production_spherical_billboard.tesc" "${SHADER_DIR}/production_spherical_billboard.tese" "${SHADER_DIR}/production_cube_surface.glsl")
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
add_custom_target(NovaCoreShaders DEPENDS "${SPIRV_DIR}/triangle.vert.spv" "${SPIRV_DIR}/triangle.frag.spv" "${SPIRV_DIR}/orbit.vert.spv" "${SPIRV_DIR}/orbit.frag.spv" "${SPIRV_DIR}/orbit_previous.frag.spv" "${SPIRV_DIR}/body_forward.frag.spv" "${SPIRV_DIR}/sas_target.frag.spv" "${SPIRV_DIR}/planetary.vert.spv" "${SPIRV_DIR}/planetary.frag.spv"    "${SPIRV_DIR}/planetary_select.comp.spv" "${SPIRV_DIR}/planetary_terrain_generate.comp.spv" "${SPIRV_DIR}/planetary_height_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_field_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_families_query.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_prepare.comp.spv" "${SPIRV_DIR}/planetary_natural_terrain_global_prepare.comp.spv"  "${SPIRV_DIR}/distant_planet.vert.spv" "${SPIRV_DIR}/distant_planet.frag.spv" "${SPIRV_DIR}/planetary_ring.vert.spv" "${SPIRV_DIR}/planetary_ring_far.frag.spv" "${SPIRV_DIR}/planetary_ring_near.frag.spv" "${SPIRV_DIR}/solar_orbit.vert.spv" "${SPIRV_DIR}/solar_orbit.frag.spv" "${SPIRV_DIR}/solar_marker.vert.spv" "${SPIRV_DIR}/solar_marker.frag.spv" "${SPIRV_DIR}/solar_label.vert.spv" "${SPIRV_DIR}/solar_label.frag.spv" "${SPIRV_DIR}/solar_speed_hud.vert.spv" "${SPIRV_DIR}/solar_speed_hud.frag.spv" "${SPIRV_DIR}/fullscreen.vert.spv" "${SPIRV_DIR}/space_background.frag.spv" "${SPIRV_DIR}/tone_map.frag.spv" "${SPIRV_DIR}/stellar_sun.vert.spv" "${SPIRV_DIR}/stellar_sun.frag.spv" "${SPIRV_DIR}/stellar_glow.vert.spv" "${SPIRV_DIR}/stellar_glow.frag.spv")

foreach(REGIONAL_SHADER regional_physical_demand.comp regional_physical_incoming_demand.comp)
  add_custom_command(OUTPUT "${SPIRV_DIR}/${REGIONAL_SHADER}.spv"
    COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/${REGIONAL_SHADER}" -o "${SPIRV_DIR}/${REGIONAL_SHADER}.spv"
    DEPENDS "${SHADER_DIR}/${REGIONAL_SHADER}" "${SHADER_DIR}/regional_physical.glsl" "${SHADER_DIR}/production_cube_surface.glsl")
endforeach()
add_custom_target(NovaCoreProductionShaders DEPENDS
  "${SPIRV_DIR}/regional_physical_demand.comp.spv"
  "${SPIRV_DIR}/regional_physical_incoming_demand.comp.spv"
  "${SPIRV_DIR}/planetary_production.frag.spv"
  "${SPIRV_DIR}/planetary_production_terrain.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_reset.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_prepare.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_prepare.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_cull.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_compact.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_reset.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_cull.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard_incoming_compact.comp.spv"
  "${SPIRV_DIR}/production_spherical_billboard.vert.spv"
  "${SPIRV_DIR}/production_spherical_billboard.tesc.spv"
  "${SPIRV_DIR}/production_spherical_billboard.tese.spv")
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

add_executable(NovaCoreRegionalPhysicalTests EXCLUDE_FROM_ALL RegionalPhysicalResidencyTests.cpp LocalTerrainPack.cpp)
add_executable(NovaCoreMappedBufferMemoryTests EXCLUDE_FROM_ALL MappedBufferMemoryTests.cpp)
target_compile_features(NovaCoreMappedBufferMemoryTests PRIVATE cxx_std_20)
target_include_directories(NovaCoreMappedBufferMemoryTests PRIVATE ${Vulkan_INCLUDE_DIRS})
target_compile_features(NovaCoreRegionalPhysicalTests PRIVATE cxx_std_20)
target_compile_definitions(NovaCoreRegionalPhysicalTests PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(NovaCoreRegionalPhysicalTests PRIVATE bcrypt)
target_compile_features(NovaCore.Native PRIVATE cxx_std_20)
target_compile_definitions(NovaCore.Native PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX VK_USE_PLATFORM_WIN32_KHR NC_NATIVE_EXPORTS $<$<CONFIG:Debug>:NC_DEBUG_BUILD=1>)
target_link_libraries(NovaCore.Native PRIVATE Vulkan::Vulkan user32 bcrypt)
add_custom_command(TARGET NovaCore.Native POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different "$<TARGET_FILE:NovaCore.Native>" "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/NovaCore.Native.dll")

# Permanent analytical visibility parity test; not a runtime compute pass.
add_custom_command(OUTPUT "${SPIRV_DIR}/facility_visibility_test.comp.spv"
  COMMAND ${GLSLC_EXECUTABLE} -I "${SHADER_DIR}" "${SHADER_DIR}/facility_visibility_test.comp" -o "${SPIRV_DIR}/facility_visibility_test.comp.spv"
  DEPENDS "${SHADER_DIR}/facility_visibility_test.comp" "${SHADER_DIR}/facility_light_occlusion.glsl")
add_custom_target(NovaCoreFacilityVisibilityTestShader DEPENDS "${SPIRV_DIR}/facility_visibility_test.comp.spv")
add_executable(NovaCoreFacilityVisibilityTests EXCLUDE_FROM_ALL FacilityLightOcclusionTests.cpp)
add_dependencies(NovaCoreFacilityVisibilityTests NovaCoreFacilityVisibilityTestShader)
target_compile_features(NovaCoreFacilityVisibilityTests PRIVATE cxx_std_20)
target_compile_definitions(NovaCoreFacilityVisibilityTests PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(NovaCoreFacilityVisibilityTests PRIVATE Vulkan::Vulkan)

# Body-fixed material receiver/precision contract, using the production GLSL helper.
add_custom_command(OUTPUT "${SPIRV_DIR}/surface_material_coordinates_test.comp.spv"
  COMMAND "${GLSLC_EXECUTABLE}" --target-env=vulkan1.1 -I "${SHADER_DIR}" "${SHADER_DIR}/surface_material_coordinates_test.comp" -o "${SPIRV_DIR}/surface_material_coordinates_test.comp.spv"
  DEPENDS "${SHADER_DIR}/surface_material_coordinates_test.comp" "${SHADER_DIR}/surface_material_coordinates.glsl")
add_custom_target(NovaCoreSurfaceMaterialTestShader DEPENDS "${SPIRV_DIR}/surface_material_coordinates_test.comp.spv")
add_executable(NovaCoreSurfaceMaterialCoordinatesTests EXCLUDE_FROM_ALL SurfaceMaterialCoordinatesTests.cpp)
add_dependencies(NovaCoreSurfaceMaterialCoordinatesTests NovaCoreSurfaceMaterialTestShader)
target_compile_features(NovaCoreSurfaceMaterialCoordinatesTests PRIVATE cxx_std_20)
target_compile_definitions(NovaCoreSurfaceMaterialCoordinatesTests PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(NovaCoreSurfaceMaterialCoordinatesTests PRIVATE Vulkan::Vulkan)

# Actual stellar/background production shaders, rendered offscreen under camera rotations.
add_executable(NovaCoreStellarProjectionTests EXCLUDE_FROM_ALL StellarProjectionTests.cpp)
add_dependencies(NovaCoreStellarProjectionTests NovaCoreShaders)
target_compile_features(NovaCoreStellarProjectionTests PRIVATE cxx_std_20)
target_link_libraries(NovaCoreStellarProjectionTests PRIVATE Vulkan::Vulkan)
