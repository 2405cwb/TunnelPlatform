#include "ApiClient.h"

#include <QJsonParseError>
#include <QNetworkReply>
#include <QNetworkRequest>
#include <QScopeGuard>
#include <QUrl>

ApiClient::ApiClient(QObject *parent)
    : QObject(parent)
{
}

void ApiClient::setBaseUrl(const QString &baseUrl)
{
    // 保存时统一去掉末尾斜杠，后续 makeUrl 可以稳定拼接相对路径。
    baseUrl_ = baseUrl.trimmed();
    while (baseUrl_.endsWith('/')) {
        baseUrl_.chop(1);
    }

    if (baseUrl_.isEmpty()) {
        baseUrl_ = "http://localhost:5140";
    }
}

QString ApiClient::baseUrl() const
{
    return baseUrl_;
}

void ApiClient::getProjectInstances(JsonCallback onSuccess, ErrorCallback onError)
{
    getJson("/api/query/project-instances", std::move(onSuccess), std::move(onError));
}

void ApiClient::getProjectOverview(const QString &projectId, JsonCallback onSuccess, ErrorCallback onError)
{
    getJson(QString("/api/query/projects/%1/overview").arg(projectId), std::move(onSuccess), std::move(onError));
}

void ApiClient::getProjectEntities(const QString &projectId, JsonCallback onSuccess, ErrorCallback onError)
{
    getJson(QString("/api/query/projects/%1/entities").arg(projectId), std::move(onSuccess), std::move(onError));
}

void ApiClient::getEntityGrayImages(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError)
{
    getJson(QString("/api/query/projects/%1/entities/%2/gray-images").arg(projectId, entityId), std::move(onSuccess), std::move(onError));
}

void ApiClient::getEntityDiseases(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError)
{
    getJson(QString("/api/query/projects/%1/entities/%2/diseases").arg(projectId, entityId), std::move(onSuccess), std::move(onError));
}

void ApiClient::getDiseaseStatistics(const QString &projectId, const QString &entityId, JsonCallback onSuccess, ErrorCallback onError)
{
    getJson(QString("/api/query/projects/%1/disease-statistics?entityId=%2").arg(projectId, entityId), std::move(onSuccess), std::move(onError));
}

void ApiClient::getImageBytes(const QString &fileUrl, BytesCallback onSuccess, ErrorCallback onError)
{
    getBytes(fileUrl, std::move(onSuccess), std::move(onError));
}

QUrl ApiClient::makeUrl(const QString &path) const
{
    // 后端返回的图片地址有可能已经是绝对 URL，这种情况直接使用。
    if (path.startsWith("http://") || path.startsWith("https://")) {
        return QUrl(path);
    }

    return QUrl(baseUrl_ + (path.startsWith('/') ? path : "/" + path));
}

void ApiClient::getJson(const QString &path, JsonCallback onSuccess, ErrorCallback onError)
{
    auto *reply = network_.get(QNetworkRequest(makeUrl(path)));
    connect(reply, &QNetworkReply::finished, this, [reply, onSuccess = std::move(onSuccess), onError = std::move(onError)]() {
        const auto guard = qScopeGuard([reply]() { reply->deleteLater(); });

        if (reply->error() != QNetworkReply::NoError) {
            onError(reply->errorString());
            return;
        }

        QJsonParseError error;
        const auto document = QJsonDocument::fromJson(reply->readAll(), &error);
        if (error.error != QJsonParseError::NoError) {
            onError("JSON 解析失败：" + error.errorString());
            return;
        }

        onSuccess(document);
    });
}

void ApiClient::getBytes(const QString &path, BytesCallback onSuccess, ErrorCallback onError)
{
    auto *reply = network_.get(QNetworkRequest(makeUrl(path)));
    connect(reply, &QNetworkReply::finished, this, [reply, onSuccess = std::move(onSuccess), onError = std::move(onError)]() {
        const auto guard = qScopeGuard([reply]() { reply->deleteLater(); });

        if (reply->error() != QNetworkReply::NoError) {
            onError(reply->errorString());
            return;
        }

        onSuccess(reply->readAll());
    });
}
