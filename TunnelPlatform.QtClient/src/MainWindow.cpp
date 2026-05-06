#include "MainWindow.h"

#include <QAbstractItemView>
#include <QComboBox>
#include <QFrame>
#include <QGridLayout>
#include <QGroupBox>
#include <QHBoxLayout>
#include <QHeaderView>
#include <QJsonDocument>
#include <QJsonValue>
#include <QLabel>
#include <QLineEdit>
#include <QListWidget>
#include <QPixmap>
#include <QPushButton>
#include <QScrollArea>
#include <QScrollBar>
#include <QSplitter>
#include <QTableWidget>
#include <QVBoxLayout>

namespace
{
constexpr auto ProjectIdRole = Qt::UserRole + 1;
constexpr auto EntityIdRole = Qt::UserRole + 2;
constexpr int ImageCardWidth = 420;
constexpr int ImageCardHeight = 360;

QFrame *makePanelFrame(const QString &objectName = "panel")
{
    auto *panel = new QFrame();
    panel->setObjectName(objectName);
    return panel;
}

QLabel *makeMetricLabel(const QString &text)
{
    auto *label = new QLabel(text);
    label->setObjectName("metricValue");
    label->setAlignment(Qt::AlignLeft | Qt::AlignVCenter);
    return label;
}

QFrame *makeImageCard(const QString &title, QLabel **imageLabel)
{
    auto *card = new QFrame();
    card->setObjectName("imageCard");
    card->setFixedSize(ImageCardWidth, ImageCardHeight);

    auto *layout = new QVBoxLayout(card);
    layout->setContentsMargins(10, 10, 10, 10);
    layout->setSpacing(8);

    auto *caption = new QLabel(title);
    caption->setObjectName("imageCaption");
    caption->setWordWrap(false);
    caption->setMinimumHeight(22);
    layout->addWidget(caption);

    *imageLabel = new QLabel("加载中...");
    (*imageLabel)->setObjectName("stripImage");
    (*imageLabel)->setAlignment(Qt::AlignCenter);
    (*imageLabel)->setMinimumSize(ImageCardWidth - 20, ImageCardHeight - 58);
    layout->addWidget(*imageLabel, 1);

    return card;
}
}

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
{
    buildUi();
    applyTheme();
    refreshProjects();
}

void MainWindow::buildUi()
{
    auto *root = new QWidget(this);
    auto *rootLayout = new QVBoxLayout(root);
    rootLayout->setContentsMargins(18, 18, 18, 14);
    rootLayout->setSpacing(14);

    auto *topBar = makePanelFrame("topBar");
    auto *topLayout = new QHBoxLayout(topBar);
    topLayout->setContentsMargins(18, 12, 18, 12);
    topLayout->setSpacing(12);

    auto *brandLayout = new QVBoxLayout();
    brandLayout->setSpacing(2);
    auto *eyebrow = new QLabel("Tunnel Inspection Console");
    eyebrow->setObjectName("eyebrow");
    titleLabel_ = new QLabel("地铁隧道展示平台");
    titleLabel_->setObjectName("appTitle");
    brandLayout->addWidget(eyebrow);
    brandLayout->addWidget(titleLabel_);
    topLayout->addLayout(brandLayout, 1);

    apiUrlEdit_ = new QLineEdit("http://localhost:5140");
    apiUrlEdit_->setMinimumWidth(270);
    apiUrlEdit_->setPlaceholderText("API 地址");
    topLayout->addWidget(apiUrlEdit_);

    projectCombo_ = new QComboBox();
    projectCombo_->setMinimumWidth(340);
    topLayout->addWidget(projectCombo_);

    refreshButton_ = new QPushButton("刷新");
    topLayout->addWidget(refreshButton_);
    rootLayout->addWidget(topBar);

    auto *mainSplitter = new QSplitter(Qt::Horizontal, root);
    mainSplitter->setObjectName("mainSplitter");

    auto *leftPanel = makePanelFrame();
    auto *leftLayout = new QVBoxLayout(leftPanel);
    leftLayout->setContentsMargins(14, 14, 14, 14);
    auto *leftTitle = new QLabel("站点 / 区间");
    leftTitle->setObjectName("panelTitle");
    entityList_ = new QListWidget();
    entityList_->setAlternatingRowColors(false);
    leftLayout->addWidget(leftTitle);
    leftLayout->addWidget(entityList_, 1);
    mainSplitter->addWidget(leftPanel);

    auto *centerPanel = makePanelFrame();
    auto *centerLayout = new QVBoxLayout(centerPanel);
    centerLayout->setContentsMargins(14, 14, 14, 14);
    centerLayout->setSpacing(10);

    auto *centerHeader = new QHBoxLayout();
    auto *centerTitle = new QLabel("二维连续浏览");
    centerTitle->setObjectName("panelTitle");
    metaLabel_ = new QLabel("选择站点/区间后横向连续浏览灰度图。");
    metaLabel_->setObjectName("mutedText");
    centerHeader->addWidget(centerTitle);
    centerHeader->addStretch(1);
    centerHeader->addWidget(metaLabel_);
    centerLayout->addLayout(centerHeader);

    imageScroll_ = new QScrollArea();
    imageScroll_->setWidgetResizable(true);
    imageScroll_->setObjectName("imageScroll");
    imageScroll_->setHorizontalScrollBarPolicy(Qt::ScrollBarAsNeeded);
    imageScroll_->setVerticalScrollBarPolicy(Qt::ScrollBarAlwaysOff);

    imageStrip_ = new QWidget();
    imageStrip_->setObjectName("imageStrip");
    imageStripLayout_ = new QHBoxLayout(imageStrip_);
    imageStripLayout_->setContentsMargins(14, 14, 14, 14);
    imageStripLayout_->setSpacing(12);
    imageStripLayout_->addStretch(1);
    imageScroll_->setWidget(imageStrip_);
    centerLayout->addWidget(imageScroll_, 1);

    auto *imageControls = new QHBoxLayout();
    prevImageButton_ = new QPushButton("上一组");
    nextImageButton_ = new QPushButton("下一组");
    imageCounterLabel_ = new QLabel("0 / 0");
    imageCounterLabel_->setAlignment(Qt::AlignCenter);
    imageControls->addWidget(prevImageButton_);
    imageControls->addWidget(imageCounterLabel_, 1);
    imageControls->addWidget(nextImageButton_);
    centerLayout->addLayout(imageControls);
    mainSplitter->addWidget(centerPanel);

    auto *rightPanel = makePanelFrame();
    auto *rightLayout = new QVBoxLayout(rightPanel);
    rightLayout->setContentsMargins(14, 14, 14, 14);
    auto *rightTitle = new QLabel("工程信息");
    rightTitle->setObjectName("panelTitle");
    rightLayout->addWidget(rightTitle);

    auto *metricsLayout = new QGridLayout();
    metricsLayout->setSpacing(8);
    entityCountLabel_ = makeMetricLabel("实体 0");
    uploadedCountLabel_ = makeMetricLabel("已上传 0");
    diseaseCountLabel_ = makeMetricLabel("病害 0");
    grayImageCountLabel_ = makeMetricLabel("灰度图 0");
    ringCountLabel_ = makeMetricLabel("环片 0");
    pointCloudCountLabel_ = makeMetricLabel("点云 0");
    metricsLayout->addWidget(entityCountLabel_, 0, 0);
    metricsLayout->addWidget(uploadedCountLabel_, 0, 1);
    metricsLayout->addWidget(diseaseCountLabel_, 1, 0);
    metricsLayout->addWidget(grayImageCountLabel_, 1, 1);
    metricsLayout->addWidget(ringCountLabel_, 2, 0);
    metricsLayout->addWidget(pointCloudCountLabel_, 2, 1);
    rightLayout->addLayout(metricsLayout);

    statsTable_ = new QTableWidget(0, 3);
    statsTable_->setHorizontalHeaderLabels({ "病害类型", "数量", "里程范围" });
    statsTable_->horizontalHeader()->setStretchLastSection(true);
    statsTable_->verticalHeader()->setVisible(false);
    statsTable_->setEditTriggers(QAbstractItemView::NoEditTriggers);
    statsTable_->setSelectionBehavior(QAbstractItemView::SelectRows);
    rightLayout->addWidget(statsTable_, 1);
    mainSplitter->addWidget(rightPanel);

    mainSplitter->setStretchFactor(0, 0);
    mainSplitter->setStretchFactor(1, 1);
    mainSplitter->setStretchFactor(2, 0);
    mainSplitter->setSizes({ 320, 820, 360 });
    rootLayout->addWidget(mainSplitter, 1);

    auto *bottomPanel = makePanelFrame();
    auto *bottomLayout = new QVBoxLayout(bottomPanel);
    bottomLayout->setContentsMargins(14, 14, 14, 14);
    auto *bottomTitle = new QLabel("病害列表");
    bottomTitle->setObjectName("panelTitle");
    diseaseTable_ = new QTableWidget(0, 5);
    diseaseTable_->setHorizontalHeaderLabels({ "类型", "中心里程", "范围", "等级", "图像" });
    diseaseTable_->horizontalHeader()->setStretchLastSection(true);
    diseaseTable_->verticalHeader()->setVisible(false);
    diseaseTable_->setEditTriggers(QAbstractItemView::NoEditTriggers);
    diseaseTable_->setSelectionBehavior(QAbstractItemView::SelectRows);
    bottomLayout->addWidget(bottomTitle);
    bottomLayout->addWidget(diseaseTable_);
    bottomPanel->setMaximumHeight(240);
    rootLayout->addWidget(bottomPanel);

    statusLabel_ = new QLabel("就绪");
    statusLabel_->setObjectName("statusLabel");
    rootLayout->addWidget(statusLabel_);

    setCentralWidget(root);

    connect(refreshButton_, &QPushButton::clicked, this, &MainWindow::refreshProjects);
    connect(projectCombo_, &QComboBox::currentIndexChanged, this, &MainWindow::onProjectChanged);
    connect(entityList_, &QListWidget::currentItemChanged, this, &MainWindow::onEntityChanged);
    connect(prevImageButton_, &QPushButton::clicked, this, &MainWindow::showPreviousImageGroup);
    connect(nextImageButton_, &QPushButton::clicked, this, &MainWindow::showNextImageGroup);
}

void MainWindow::applyTheme()
{
    setStyleSheet(R"(
        QMainWindow, QWidget {
            background: #101318;
            color: #e8edf2;
            font-family: "Microsoft YaHei UI", "Segoe UI";
            font-size: 14px;
        }
        #topBar, #panel {
            background: #171d25;
            border: 1px solid #303a49;
            border-radius: 8px;
        }
        #topBar {
            background: #151a21;
        }
        #eyebrow {
            color: #8fa3b8;
            font-size: 11px;
            letter-spacing: 0px;
        }
        #appTitle {
            font-size: 24px;
            font-weight: 700;
        }
        #panelTitle {
            color: #f4d06f;
            font-size: 15px;
            font-weight: 700;
        }
        #mutedText, #statusLabel {
            color: #9aa8b6;
        }
        QLineEdit, QComboBox {
            background: #0f1318;
            border: 1px solid #303846;
            border-radius: 6px;
            padding: 7px 10px;
            color: #f1f5f9;
        }
        QPushButton {
            background: #d6b45f;
            color: #121417;
            border: 0;
            border-radius: 6px;
            padding: 8px 14px;
            font-weight: 600;
        }
        QPushButton:disabled {
            background: #48505c;
            color: #9aa3ad;
        }
        QListWidget, QTableWidget, QScrollArea {
            background: #0d1116;
            border: 1px solid #2b313b;
            border-radius: 6px;
        }
        QListWidget::item {
            padding: 10px 8px;
            border-bottom: 1px solid #202630;
        }
        QListWidget::item:selected {
            background: #263449;
            color: #ffffff;
        }
        QHeaderView::section {
            background: #1e2530;
            color: #d6dee8;
            border: 0;
            padding: 8px;
        }
        #metricValue {
            background: #101722;
            border: 1px solid #2b3948;
            border-radius: 6px;
            padding: 10px;
            font-size: 15px;
            font-weight: 600;
        }
        #imageScroll, #imageStrip {
            background: #080b0f;
        }
        #imageCard {
            background: #111821;
            border: 1px solid #303b49;
            border-radius: 8px;
        }
        #imageCaption {
            color: #dce5ee;
            font-size: 12px;
        }
        #stripImage {
            background: #06080b;
            color: #667483;
            border-radius: 6px;
        }
    )");
}

void MainWindow::refreshProjects()
{
    setBusy(true);
    setStatus("正在加载工程实例...");
    apiClient_.setBaseUrl(apiUrlEdit_->text());
    apiClient_.getProjectInstances(
        [this](const QJsonDocument &doc) {
            loadProjectsFromArray(doc.array());
            setBusy(false);
            setStatus(QString("已加载 %1 个工程实例").arg(projects_.size()));
        },
        [this](const QString &message) {
            handleRequestError(message);
        });
}

void MainWindow::onProjectChanged(int index)
{
    if (index < 0) {
        return;
    }

    currentProjectId_ = projectCombo_->itemData(index, ProjectIdRole).toString();
    if (currentProjectId_.isEmpty()) {
        setStatus("工程实例缺少 ID，无法加载站点/区间。");
        return;
    }

    loadProjectDetails(currentProjectId_);
    loadEntities(currentProjectId_);
}

void MainWindow::onEntityChanged(QListWidgetItem *current, QListWidgetItem *)
{
    if (!current || currentProjectId_.isEmpty()) {
        return;
    }

    currentEntityId_ = current->data(EntityIdRole).toString();
    if (currentEntityId_.isEmpty()) {
        setStatus("站点/区间缺少 ID，无法加载详情。");
        return;
    }

    loadEntityDetails(currentProjectId_, currentEntityId_);
}

void MainWindow::showPreviousImageGroup()
{
    if (grayImages_.isEmpty()) {
        return;
    }

    scrollToImage(qMax(0, currentImageIndex_ - 3));
}

void MainWindow::showNextImageGroup()
{
    if (grayImages_.isEmpty()) {
        return;
    }

    scrollToImage(qMin(grayImages_.size() - 1, currentImageIndex_ + 3));
}

void MainWindow::setBusy(bool busy)
{
    refreshButton_->setEnabled(!busy);
    apiUrlEdit_->setEnabled(!busy);
}

void MainWindow::setStatus(const QString &message)
{
    statusLabel_->setText(message);
}

void MainWindow::handleRequestError(const QString &message)
{
    setBusy(false);
    setStatus("请求失败：" + message);
}

void MainWindow::loadProjectsFromArray(const QJsonArray &projects)
{
    projects_ = projects;
    projectCombo_->blockSignals(true);
    projectCombo_->clear();

    for (const auto &value : projects_) {
        const auto object = value.toObject();
        const auto name = QString("%1 / %2 / %3")
            .arg(displayText(object, "projectName"))
            .arg(displayText(object, "direction"))
            .arg(displayText(object, "collectionDate"));
        projectCombo_->addItem(name);
        projectCombo_->setItemData(projectCombo_->count() - 1, object.value("projectId").toString(), ProjectIdRole);
    }

    projectCombo_->blockSignals(false);
    if (projectCombo_->count() > 0) {
        projectCombo_->setCurrentIndex(0);
        onProjectChanged(0);
    }
}

void MainWindow::loadProjectDetails(const QString &projectId)
{
    apiClient_.getProjectOverview(
        projectId,
        [this](const QJsonDocument &doc) {
            updateOverview(doc.object());
        },
        [this](const QString &message) {
            handleRequestError(message);
        });
}

void MainWindow::loadEntities(const QString &projectId)
{
    setStatus("正在加载站点/区间...");
    entityList_->clear();
    entities_ = {};
    apiClient_.getProjectEntities(
        projectId,
        [this](const QJsonDocument &doc) {
            entities_ = doc.array();
            for (const auto &value : entities_) {
                const auto object = value.toObject();
                auto *item = new QListWidgetItem(QString("%1\n%2 - %3")
                    .arg(displayText(object, "displayName"))
                    .arg(mileageText(object.value("beginMileage")))
                    .arg(mileageText(object.value("endMileage"))));
                item->setData(EntityIdRole, object.value("entityId").toString());
                entityList_->addItem(item);
            }

            if (entityList_->count() > 0) {
                entityList_->setCurrentRow(0);
                setStatus(QString("已加载 %1 个站点/区间").arg(entityList_->count()));
            } else {
                setStatus("当前工程没有站点/区间数据。");
            }
        },
        [this](const QString &message) {
            handleRequestError(message);
        });
}

void MainWindow::loadEntityDetails(const QString &projectId, const QString &entityId)
{
    metaLabel_->setText("正在加载灰度图和病害记录...");
    grayImages_ = {};
    currentImageIndex_ = -1;
    updateImageControls();

    apiClient_.getEntityGrayImages(
        projectId,
        entityId,
        [this](const QJsonDocument &doc) {
            grayImages_ = doc.array();
            currentImageIndex_ = grayImages_.isEmpty() ? -1 : 0;
            buildImageStrip();
            setStatus(grayImages_.isEmpty()
                ? "当前区间暂无灰度图。"
                : QString("已加载 %1 张灰度图").arg(grayImages_.size()));
        },
        [this](const QString &message) {
            handleRequestError(message);
        });

    apiClient_.getEntityDiseases(
        projectId,
        entityId,
        [this](const QJsonDocument &doc) {
            updateDiseases(doc.array());
        },
        [this](const QString &message) {
            handleRequestError(message);
        });

    apiClient_.getDiseaseStatistics(
        projectId,
        entityId,
        [this](const QJsonDocument &doc) {
            updateStats(doc.array());
        },
        [this](const QString &message) {
            handleRequestError(message);
        });
}

void MainWindow::buildImageStrip()
{
    // 连续浏览条每次按当前区间重建。卡片尺寸固定，避免图片异步加载后界面跳动。
    while (auto *item = imageStripLayout_->takeAt(0)) {
        if (auto *widget = item->widget()) {
            widget->deleteLater();
        }
        delete item;
    }

    if (grayImages_.isEmpty()) {
        auto *empty = new QLabel("当前区间暂无灰度图");
        empty->setObjectName("stripImage");
        empty->setAlignment(Qt::AlignCenter);
        empty->setMinimumSize(760, ImageCardHeight);
        imageStripLayout_->addWidget(empty);
        imageStripLayout_->addStretch(1);
        updateImageControls();
        return;
    }

    for (int index = 0; index < grayImages_.size(); ++index) {
        const auto object = grayImages_.at(index).toObject();
        const auto title = QString("%1  %2 - %3")
            .arg(displayText(object, "fileName"))
            .arg(mileageText(object.value("beginMileage")))
            .arg(mileageText(object.value("endMileage")));

        QLabel *cardImage = nullptr;
        auto *card = makeImageCard(title, &cardImage);
        imageStripLayout_->addWidget(card);
        loadImageCard(index, cardImage);
    }

    imageStripLayout_->addStretch(1);
    updateImageControls();
    scrollToImage(0);
}

void MainWindow::loadImageCard(int index, QLabel *imageLabel)
{
    if (index < 0 || index >= grayImages_.size()) {
        return;
    }

    const auto object = grayImages_.at(index).toObject();
    const auto fileUrl = object.value("fileUrl").toString();
    if (fileUrl.isEmpty()) {
        imageLabel->setText("图片缺少 fileUrl");
        return;
    }

    // 下载仍由 ApiClient 处理；这里仅把返回字节解码为 QPixmap 并显示。
    apiClient_.getImageBytes(
        fileUrl,
        [imageLabel](const QByteArray &bytes) {
            QPixmap pixmap;
            if (!pixmap.loadFromData(bytes)) {
                imageLabel->setText("无法解码图片");
                return;
            }

            imageLabel->setPixmap(pixmap.scaled(
                imageLabel->size(),
                Qt::KeepAspectRatio,
                Qt::SmoothTransformation));
        },
        [imageLabel](const QString &message) {
            imageLabel->setText("图片加载失败：" + message);
        });
}

void MainWindow::scrollToImage(int index)
{
    if (index < 0 || index >= grayImages_.size()) {
        return;
    }

    currentImageIndex_ = index;
    const auto targetX = index * (ImageCardWidth + imageStripLayout_->spacing());
    imageScroll_->horizontalScrollBar()->setValue(targetX);
    updateImageControls();
}

void MainWindow::updateOverview(const QJsonObject &overview)
{
    const auto project = overview.value("project").toObject();
    titleLabel_->setText(displayText(project, "projectName"));
    entityCountLabel_->setText("实体 " + numberText(project, "entityCount"));
    uploadedCountLabel_->setText("已上传 " + numberText(project, "uploadedEntityCount"));
    diseaseCountLabel_->setText("病害 " + numberText(overview, "diseaseCount"));
    grayImageCountLabel_->setText("灰度图 " + numberText(overview, "grayImageCount"));
    ringCountLabel_->setText("环片 " + numberText(overview, "ringCount"));
    pointCloudCountLabel_->setText("点云 " + numberText(overview, "pointCloudFileCount"));
    updateStats(overview.value("diseaseStatistics").toArray());
}

void MainWindow::updateStats(const QJsonArray &stats)
{
    statsTable_->setRowCount(stats.size());
    for (int row = 0; row < stats.size(); ++row) {
        const auto object = stats.at(row).toObject();
        statsTable_->setItem(row, 0, new QTableWidgetItem(displayText(object, "diseaseType")));
        statsTable_->setItem(row, 1, new QTableWidgetItem(numberText(object, "count")));
        statsTable_->setItem(row, 2, new QTableWidgetItem(QString("%1 - %2")
            .arg(mileageText(object.value("minMileage")))
            .arg(mileageText(object.value("maxMileage")))));
    }
}

void MainWindow::updateDiseases(const QJsonArray &diseases)
{
    diseaseTable_->setRowCount(diseases.size());
    for (int row = 0; row < diseases.size(); ++row) {
        const auto object = diseases.at(row).toObject();
        diseaseTable_->setItem(row, 0, new QTableWidgetItem(displayText(object, "diseaseType")));
        diseaseTable_->setItem(row, 1, new QTableWidgetItem(mileageText(object.value("mileage"))));
        diseaseTable_->setItem(row, 2, new QTableWidgetItem(QString("%1 - %2")
            .arg(mileageText(object.value("beginMileage")))
            .arg(mileageText(object.value("endMileage")))));
        diseaseTable_->setItem(row, 3, new QTableWidgetItem(numberText(object, "severity")));
        diseaseTable_->setItem(row, 4, new QTableWidgetItem(displayText(object, "imageName")));
    }

    setStatus(QString("已加载 %1 条病害记录").arg(diseases.size()));
}

void MainWindow::updateImageControls()
{
    imageCounterLabel_->setText(QString("%1 / %2")
        .arg(currentImageIndex_ >= 0 ? currentImageIndex_ + 1 : 0)
        .arg(grayImages_.size()));
    prevImageButton_->setEnabled(currentImageIndex_ > 0);
    nextImageButton_->setEnabled(currentImageIndex_ + 1 < grayImages_.size());
}

QString MainWindow::displayText(const QJsonObject &object, const QString &key)
{
    const auto value = object.value(key);
    if (value.isString()) {
        return value.toString();
    }
    if (value.isDouble()) {
        return QString::number(value.toDouble());
    }
    if (value.isBool()) {
        return value.toBool() ? "是" : "否";
    }

    return "-";
}

QString MainWindow::numberText(const QJsonObject &object, const QString &key)
{
    const auto value = object.value(key);
    if (value.isDouble()) {
        return QString::number(value.toInt());
    }
    if (value.isString()) {
        return value.toString();
    }

    return "0";
}

QString MainWindow::mileageText(const QJsonValue &value)
{
    if (!value.isDouble()) {
        return "-";
    }

    return QString::number(value.toDouble(), 'f', 3);
}
