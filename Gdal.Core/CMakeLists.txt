cmake_minimum_required(VERSION 3.13)
project(GDAL_CSharp)
set(CMAKE_AUTOMOC ON)

add_subdirectory(gdal)
add_subdirectory(const)
add_subdirectory(ogr)
add_subdirectory(osr)
