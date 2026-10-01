# Capture ownership at the declarations that also define the native build graph.
# There is no second list of runtime shader names.
if(DEFINED NC_NATIVE_FILE)
  file(SHA256 "${NC_NATIVE_FILE}" dll_hash)
  file(WRITE "${NC_NATIVE_RECEIPT}"
    "{\n  \"configuration\": \"${NC_CONFIGURATION}\",\n  \"dllSha256\": \"${dll_hash}\"\n}\n")
  return()
endif()

function(nc_shader_command)
  add_custom_command(${ARGV})
  cmake_parse_arguments(S "VERBATIM" "DEPFILE" "OUTPUT;COMMAND;DEPENDS" ${ARGV})
  foreach(output IN LISTS S_OUTPUT)
    string(MD5 key "${output}")
    set_property(GLOBAL PROPERTY "NC_SHADER_OUTPUTS_${key}" "${S_OUTPUT}")
  endforeach()
endfunction()

function(nc_shader_target name)
  add_custom_target(${name} ${ARGN})
  cmake_parse_arguments(S "" "" "DEPENDS" ${ARGN})
  set_property(TARGET ${name} PROPERTY NC_SHADER_ROOTS "${S_DEPENDS}")
endfunction()

function(nc_collect_shader_outputs target result)
  get_property(outputs TARGET ${target} PROPERTY NC_SHADER_ROOTS)
  get_property(dependencies TARGET ${target} PROPERTY MANUALLY_ADDED_DEPENDENCIES)
  foreach(dependency IN LISTS dependencies)
    nc_collect_shader_outputs(${dependency} children)
    list(APPEND outputs ${children})
  endforeach()
  set(closure)
  foreach(output IN LISTS outputs)
    string(MD5 key "${output}")
    get_property(siblings GLOBAL PROPERTY "NC_SHADER_OUTPUTS_${key}")
    if(NOT siblings)
      message(FATAL_ERROR "Shader dependency has no registered producer: ${output}")
    endif()
    list(APPEND closure ${siblings})
  endforeach()
  set(${result} "${closure}" PARENT_SCOPE)
endfunction()

function(nc_export_runtime_shaders target)
  # Existing canonical native roots are single-configuration Ninja builds.
  # Do not silently label a shared multi-config shader directory as config-specific.
  if(CMAKE_CONFIGURATION_TYPES OR NOT CMAKE_BUILD_TYPE MATCHES "^(Debug|Release)$")
    message(FATAL_ERROR "Runtime deployment requires a single Debug or Release native build root")
  endif()
  nc_collect_shader_outputs(${target} outputs)
  list(REMOVE_DUPLICATES outputs)
  list(SORT outputs)
  set(names)
  foreach(output IN LISTS outputs)
    file(RELATIVE_PATH name "${SPIRV_DIR}" "${output}")
    if(NOT name MATCHES "^[A-Za-z0-9_.-]+\\.spv$")
      message(FATAL_ERROR "Runtime shader is outside the runtime output owner: ${output}")
    endif()
    list(APPEND names "${name}")
  endforeach()
  if(NOT names)
    message(FATAL_ERROR "Empty runtime shader dependency closure")
  endif()
  file(SHA256 "${CMAKE_CURRENT_SOURCE_DIR}/CMakeLists.txt" source_hash)
  file(SHA256 "${CMAKE_CURRENT_SOURCE_DIR}/RuntimeShaderDeployment.cmake" owner_hash)
  string(JOIN "\",\n    \"" json_names ${names})
  file(GENERATE OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/runtime-shaders.json" CONTENT
"{\n  \"schema\": 1,\n  \"configuration\": \"$<CONFIG>\",\n  \"cmakeSha256\": \"${source_hash}\",\n  \"ownerSha256\": \"${owner_hash}\",\n  \"shaders\": [\n    \"${json_names}\"\n  ]\n}\n")
  # Seal the DLL at link time, not during deployment. A copied binary from another
  # configuration must not become valid merely because it sits in this build root.
  add_custom_command(TARGET ${target} POST_BUILD
    COMMAND ${CMAKE_COMMAND} "-DNC_NATIVE_FILE=$<TARGET_FILE:${target}>"
      "-DNC_NATIVE_RECEIPT=${CMAKE_CURRENT_BINARY_DIR}/native-runtime-build.json"
      "-DNC_CONFIGURATION=$<CONFIG>" -P "${CMAKE_CURRENT_SOURCE_DIR}/RuntimeShaderDeployment.cmake"
    VERBATIM)
endfunction()
