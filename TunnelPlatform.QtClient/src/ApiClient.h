#pragma once

#include <QByteArray>
#include <QJsonDocument>
#include <QNetworkAccessManager>
#include <QObject>
#include <QString>

#include <functional>

// ApiClient 集中封装桌面端对 TunnelPlatform.Api 的 HTTP 访问。
// MainWindow 不直接处理 QNetworkReply，只接收解析后的 JSON 或图片字节。
class ApiClient final : public QObject
{
    Q_OBJECT

public:
    using JsonCallback = std::function<void(const QJsonDocument &)>;
    using BytesCallback = std::function<void(const QByteArray &)>;
    using ErrorCallback = std::function<void(const QString &)>;

    explicit ApiClient(QObject *parent = nullptr);

    void setBaseUrl(const QString &baseUrl);
    QString baseUrl() const;

    // GET /api/query/project-instances
    void getProjectInstances(JsonCallback onSuccess, ErrorCallback onError);

    // GET /api/query/projects/{projectId}/overview
    void getProjectOverview(const QString &projectId, JsonCallback onSuccess, ErrorCallback onError);

    // GET /api/query/projects/{projectId}/entities
    void getProjectEntities(const QString &projectId, JsonCallback onSuccess, ErrorCallback onError);

    // GET /api/query/projects/{projectId}/entities/{entityId}/gray-images
    void getEntityGrayImages(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError);

    // GET /api/query/projects/{projectId}/entities/{entityId}/diseases
    void getEntityDiseases(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError);

    // GET /api/query/projects/{projectId}/disease-statistics?entityId={entityId}
    void getDiseaseStatistics(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError);

    // 图片文件走 /storage/... 或绝对 URL，返回原始字节给 UI 解码。
    void getImageBytes(const QString &fileUrl, BytesCallback onSuccess, ErrorCallback onError);

private:
    QUrl makeUrl(const QString &path) const;

    // 通用 GET JSON 封装：统一处理网络错误和 JSON 解析错误。
    void getJson(const QString &path, JsonCallback onSuccess, ErrorCallback onError);

    // 通用 GET bytes 封装：用于图片、后续点云切片等二进制资源。
    void getBytes(const QString &path, BytesCallback onSuccess, ErrorCallback onError);

    QString baseUrl_ = "http://localhost:5140";
    QNetworkAccessManager network_;
};
