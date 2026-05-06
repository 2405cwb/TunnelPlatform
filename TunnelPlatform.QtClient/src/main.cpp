#include "MainWindow.h"

#include <QApplication>

int main(int argc, char *argv[])
{
    QApplication app(argc, argv);
    QApplication::setApplicationName("Tunnel Platform Qt Client");
    QApplication::setOrganizationName("TunnelPlatform");

    MainWindow window;
    window.resize(1440, 900);
    window.show();

    return app.exec();
}
