#pragma once

#include "ApiClient.h"

#include <QJsonArray>
#include <QJsonObject>
#include <QMainWindow>

class QComboBox;
class QGridLayout;
class QHBoxLayout;
class QLabel;
class QLineEdit;
class QListWidget;
class QListWidgetItem;
class QPushButton;
class QScrollArea;
class QTableWidget;

// MainWindow 只负责桌面界面和数据展示。
// 所有 HTTP 访问都通过 ApiClient 完成，避免 UI 层混入网络细节。
class MainWindow final : public QMainWindow
{
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);

private slots:
    void refreshProjects();
    void onProjectChanged(int index);
    void onEntityChanged(QListWidgetItem *current, QListWidgetItem *previous);
    void showPreviousImageGroup();
    void showNextImageGroup();
    void onDiseaseTableDoubleClicked(int row, int column);

private:
    void buildUi();
    void applyTheme();
    void setBusy(bool busy);
    void setStatus(const QString &message);
    void handleRequestError(const QString &message);

    void loadProjectsFromArray(const QJsonArray &projects);
    void loadProjectDetails(const QString &projectId);
    void loadEntities(const QString &projectId);
    void loadEntityDetails(const QString &projectId, const QString &entityId);

    void buildImageStrip();
    void loadImageCard(int index, QLabel *imageLabel);
    void scrollToImage(int index);
    void updateOverview(const QJsonObject &overview);
    void updateStats(const QJsonArray &stats);
    void updateDiseases(const QJsonArray &diseases);
    void openDiseaseImage(int row);
    void showDiseaseImageDialog(const QJsonObject &image, const QByteArray &bytes);
    void updateImageControls();

    static QString displayText(const QJsonObject &object, const QString &key);
    static QString numberText(const QJsonObject &object, const QString &key);
    static QString mileageText(const QJsonValue &value);

    ApiClient apiClient_;

    QLineEdit *apiUrlEdit_ = nullptr;
    QPushButton *refreshButton_ = nullptr;
    QComboBox *projectCombo_ = nullptr;
    QListWidget *entityList_ = nullptr;
    QLabel *statusLabel_ = nullptr;

    QLabel *titleLabel_ = nullptr;
    QLabel *metaLabel_ = nullptr;
    QScrollArea *imageScroll_ = nullptr;
    QWidget *imageStrip_ = nullptr;
    QHBoxLayout *imageStripLayout_ = nullptr;
    QLabel *imageCounterLabel_ = nullptr;
    QPushButton *prevImageButton_ = nullptr;
    QPushButton *nextImageButton_ = nullptr;

    QLabel *entityCountLabel_ = nullptr;
    QLabel *uploadedCountLabel_ = nullptr;
    QLabel *diseaseCountLabel_ = nullptr;
    QLabel *grayImageCountLabel_ = nullptr;
    QLabel *ringCountLabel_ = nullptr;
    QLabel *pointCloudCountLabel_ = nullptr;
    QTableWidget *statsTable_ = nullptr;
    QTableWidget *diseaseTable_ = nullptr;

    QJsonArray projects_;
    QJsonArray entities_;
    QJsonArray grayImages_;
    QJsonArray diseases_;
    QString currentProjectId_;
    QString currentEntityId_;
    int currentImageIndex_ = -1;
};
