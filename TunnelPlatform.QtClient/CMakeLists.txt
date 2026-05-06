cmake_minimum_required(VERSION 3.21)

project(TunnelPlatformQtClient VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_NO_SYSTEM_FROM_IMPORTED ON)

find_package(Qt6 6.8.3 REQUIRED COMPONENTS Widgets Network)

qt_add_executable(TunnelPlatformQtClient
    src/main.cpp
    src/ApiClient.cpp
    src/ApiClient.h
    src/MainWindow.cpp
    src/MainWindow.h
)

target_link_libraries(TunnelPlatformQtClient
    PRIVATE
        Qt6::Widgets
        Qt6::Network
)

get_filename_component(QT_CLIENT_QT_ROOT "${Qt6_DIR}/../../.." ABSOLUTE)
target_include_directories(TunnelPlatformQtClient
    BEFORE PRIVATE
        "${QT_CLIENT_QT_ROOT}/include/QtCore"
        "${QT_CLIENT_QT_ROOT}/include"
        "${QT_CLIENT_QT_ROOT}/mkspecs/win32-msvc"
        "${QT_CLIENT_QT_ROOT}/include/QtWidgets"
        "${QT_CLIENT_QT_ROOT}/include/QtGui"
        "${QT_CLIENT_QT_ROOT}/include/QtNetwork"
)

if(WIN32)
    set_target_properties(TunnelPlatformQtClient PROPERTIES WIN32_EXECUTABLE TRUE)
endif()
