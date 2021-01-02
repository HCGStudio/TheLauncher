cmake_minimum_required(VERSION 3.0.0)
project(HCGStudio.TheLauncherLogin VERSION 0.1.0)

set (CMAKE_CXX_STANDARD 20)
set (CMAKE_CXX_FLAGS "-O3 -Wall -Wextra")

find_package(PkgConfig REQUIRED)
pkg_check_modules(GTK3 REQUIRED gtkmm-3.0)
pkg_check_modules(Webkit2 REQUIRED webkit2gtk-4.0)

include_directories(${GTK3_INCLUDE_DIRS})
link_directories(${GTK3_LIBRARY_DIRS})
add_definitions(${GTK3_CFLAGS_OTHER})
include_directories(${Webkit2_INCLUDE_DIRS})
link_directories(${Webkit2_LIBRARY_DIRS})
add_definitions(${Webkit2_CFLAGS_OTHER})

add_executable(HCGStudio.TheLauncherLogin main.cpp)
target_link_libraries (HCGStudio.TheLauncherLogin  ${GTK3_LIBRARIES} ${Webkit2_LIBRARIES} )
