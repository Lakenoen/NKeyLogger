cmake_minimum_required(VERSION 3.8)
project(NKeyLoggerDLL)

set( CMAKE_CXX_STANDARD 17 )
set( CMAKE_CXX_STANDARD_REQUIRED ON )

set( SOURCE src/main.cpp )
set ( CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON )

if(MSVC)
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /ZI /Gy")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /ZI /Gy")
endif()

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
	add_compile_definitions(DEBUG)
	add_executable(${PROJECT_NAME} ${SOURCE})
else()
	add_compile_definitions(RELEASE)
	add_library(${PROJECT_NAME} SHARED ${SOURCE})
endif()