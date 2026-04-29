const state = {
    authMode: "login",
    captchaId: null,
    token: localStorage.getItem("tunnel-auth-token") || "",
    user: null,
    projects: [],
    launchProjectId: null,
    projectId: null,
    pendingEntityId: null,
    entities: [],
    entityId: null,
    overview: null,
    projectDiseaseStats: [],
    entityDiseaseStats: [],
    activeStatsScope: "entity",
    diseases: [],
    grayImages: [],
    diseaseImages: [],
    ringLocations: [],
    ringPreviewMode: "mileage",
    pointCloudFrames: [],
    pointCloudIndex: 0,
    imageIndex: 0,
    selectedDiseaseId: null,
    diseasePreviewMode: "exact",
    activeView: "image",
    wheelBusy: false,
    imageWheelDelta: 0,
    imageWheelFrame: null,
    imageScrollTimer: null,
    projectLoadId: 0,
    map: null,
    mapReady: false,
    mapOverlays: [],
};

const apiCatalog = [
    ["GET", "/api/query/project-instances", "工程实例列表"],
    ["GET", "/api/query/projects/{id}/overview", "工程概览"],
    ["GET", "/api/query/projects/{id}/entities", "站点区间"],
    ["GET", "/api/query/projects/{id}/disease-statistics", "病害统计，可传 entityId"],
    ["GET", "/api/diseases/query", "病害分页"],
    ["GET", "/api/query/projects/{id}/entities/{entityId}/gray-images", "二维灰度图"],
    ["GET", "/api/query/projects/{id}/entities/{entityId}/ring-locations", "环片位置"],
    ["GET", "/api/query/projects/{id}/entities/{entityId}/disease-images", "病害高清图"],
    ["GET", "/api/projects/{id}/entities/{entityId}/file-tree", "文件树/点云帧"],
];

const $ = (id) => document.getElementById(id);

async function requestJson(url) {
    const headers = state.token ? { Authorization: `Bearer ${state.token}` } : {};
    const response = await fetch(url, { headers });
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(error.message || response.statusText);
    }

    return response.json();
}

async function postJson(url, body) {
    const headers = { "Content-Type": "application/json" };
    if (state.token) headers.Authorization = `Bearer ${state.token}`;
    const response = await fetch(url, {
        method: "POST",
        headers,
        body: JSON.stringify(body ?? {}),
    });
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(error.message || response.statusText);
    }

    return response.status === 204 ? null : response.json();
}

function formatNumber(value, digits = 2) {
    return value === null || value === undefined ? "--" : Number(value).toFixed(digits);
}

function showToast(message) {
    const toast = $("toast");
    toast.textContent = message;
    toast.classList.add("show");
    window.clearTimeout(showToast.timer);
    showToast.timer = window.setTimeout(() => toast.classList.remove("show"), 2600);
}

async function initializeAuth() {
    if (state.token) {
        try {
            state.user = await requestJson("/api/auth/me");
            showLaunch();
            await loadProjects();
            return;
        } catch {
            localStorage.removeItem("tunnel-auth-token");
            state.token = "";
        }
    }

    showAuth();
    await refreshCaptcha();
}

function showAuth() {
    $("authScreen").classList.remove("hidden");
    $("launchScreen").classList.add("hidden");
    document.querySelector(".shell").classList.add("hidden");
}

function showLaunch() {
    $("authScreen").classList.add("hidden");
    $("launchScreen").classList.remove("hidden");
    document.querySelector(".shell").classList.add("hidden");
    window.setTimeout(() => {
        if (state.mapReady && state.map) {
            state.map.checkResize?.();
            renderProjectMap();
        }
    }, 80);
}

function showDetail() {
    $("authScreen").classList.add("hidden");
    $("launchScreen").classList.add("hidden");
    document.querySelector(".shell").classList.remove("hidden");
}

function setAuthMode(mode) {
    state.authMode = mode;
    $("loginModeButton").classList.toggle("active", mode === "login");
    $("registerModeButton").classList.toggle("active", mode === "register");
    $("authSubmitButton").textContent = mode === "login" ? "登录平台" : "注册并进入";
}

async function refreshCaptcha() {
    try {
        const captcha = await requestJson("/api/auth/captcha");
        state.captchaId = captcha.captchaId;
        $("captchaImage").src = captcha.imageDataUrl;
        $("captchaCode").value = "";
    } catch (error) {
        showToast(error.message);
    }
}

async function submitAuth(event) {
    event.preventDefault();
    const payload = {
        userName: $("authUserName").value,
        password: $("authPassword").value,
        captchaId: state.captchaId,
        captchaCode: $("captchaCode").value,
    };

    try {
        const result = await postJson(state.authMode === "login" ? "/api/auth/login" : "/api/auth/register", payload);
        state.token = result.token;
        state.user = result.user;
        localStorage.setItem("tunnel-auth-token", result.token);
        showToast(`${state.user.displayName}，欢迎进入平台`);
        showLaunch();
        await loadProjects();
    } catch (error) {
        showToast(error.message);
        await refreshCaptcha();
    }
}

async function logout() {
    try {
        if (state.token) {
            await postJson("/api/auth/logout");
        }
    } catch {
        // 本地退出优先，后端会话过期时不阻塞界面。
    }

    state.token = "";
    state.user = null;
    localStorage.removeItem("tunnel-auth-token");
    showAuth();
    await refreshCaptcha();
}

async function initialize() {
    applyTheme(localStorage.getItem("tunnel-theme") || "dark");
    bindEvents();
    renderApiCatalog();
    initializeProjectMap();
    await initializeAuth();
}

function bindEvents() {
    $("loginModeButton").addEventListener("click", () => setAuthMode("login"));
    $("registerModeButton").addEventListener("click", () => setAuthMode("register"));
    $("captchaButton").addEventListener("click", refreshCaptcha);
    $("authForm").addEventListener("submit", submitAuth);
    $("lineSelect").addEventListener("change", handleLineSelectChange);
    $("dateSelect").addEventListener("change", (event) => previewProject(event.target.value));
    $("enterProjectButton").addEventListener("click", () => openProjectDetail(state.launchProjectId));
    $("logoutButton").addEventListener("click", logout);
    $("backToMapButton").addEventListener("click", showLaunch);
    $("refreshButton").addEventListener("click", () => state.projectId ? selectProject(state.projectId) : loadProjects());
    $("projectSelect").addEventListener("change", (event) => selectProject(event.target.value));
    $("themeSelect").addEventListener("change", (event) => applyTheme(event.target.value));
    $("prevImageButton").addEventListener("click", () => selectImage(state.imageIndex - 1));
    $("nextImageButton").addEventListener("click", () => selectImage(state.imageIndex + 1));
    $("imageViewTab").addEventListener("click", () => setActiveView("image"));
    $("pointCloudViewTab").addEventListener("click", () => setActiveView("pointcloud"));
    $("ringToggle").addEventListener("change", () => {
        renderRingOverlay();
        renderDiseaseOverlay();
    });
    $("diseaseTypeFilter").addEventListener("change", loadDiseases);
    $("mileageStart").addEventListener("change", loadDiseases);
    $("mileageEnd").addEventListener("change", loadDiseases);
    $("entityStatsButton").addEventListener("click", () => setStatsScope("entity"));
    $("projectStatsButton").addEventListener("click", () => setStatsScope("project"));
    $("pointCloudSlider").addEventListener("input", (event) => selectPointCloudFrame(Number(event.target.value)));
    $("closeDialogButton").addEventListener("click", () => $("imageDialog").close());
    $("imageStage").addEventListener("wheel", handleImageWheel, { passive: false });
    $("imageStage").addEventListener("scroll", syncImageIndexFromScroll, { passive: true });
    $("thumbnailProgress").addEventListener("pointermove", handleProgressHover);
    $("thumbnailProgress").addEventListener("pointerleave", hideProgressPreview);
    $("thumbnailProgress").addEventListener("click", handleProgressClick);
    $("pointCloudStage").addEventListener("wheel", handlePointCloudWheel, { passive: false });
}

function applyTheme(theme) {
    document.body.dataset.theme = theme;
    $("themeSelect").value = theme;
    localStorage.setItem("tunnel-theme", theme);
}

function setActiveView(view) {
    state.activeView = view;
    $("imageViewTab").classList.toggle("active", view === "image");
    $("pointCloudViewTab").classList.toggle("active", view === "pointcloud");
    $("imageStage").classList.toggle("active", view === "image");
    $("pointCloudStage").classList.toggle("active", view === "pointcloud");
    $("imageTimeline").style.display = view === "image" ? "grid" : "none";
    $("prevImageButton").style.display = view === "image" ? "inline-flex" : "none";
    $("nextImageButton").style.display = view === "image" ? "inline-flex" : "none";
    $("ringToggle").closest(".switch").style.display = view === "image" ? "inline-flex" : "none";
    $("viewerTitle").textContent = view === "image" ? "二维连续浏览" : "点云断面浏览";
    updateViewerMeta();
}

function handleImageWheel(event) {
    const stage = $("imageStage");
    if (state.grayImages.length <= 1 || !stage) {
        return;
    }

    event.preventDefault();
    state.imageWheelDelta += (event.deltaY || event.deltaX) * 1.15;

    if (state.imageWheelFrame !== null) {
        return;
    }

    state.imageWheelFrame = window.requestAnimationFrame(() => {
        stage.scrollLeft += state.imageWheelDelta;
        state.imageWheelDelta = 0;
        state.imageWheelFrame = null;
    });
}

function handlePointCloudWheel(event) {
    if (state.pointCloudFrames.length <= 1) {
        return;
    }

    event.preventDefault();
    if (state.wheelBusy) {
        return;
    }

    state.wheelBusy = true;
    window.setTimeout(() => {
        state.wheelBusy = false;
    }, 150);

    selectPointCloudFrame(state.pointCloudIndex + (event.deltaY > 0 ? 1 : -1));
}

async function loadProjects(preferredProjectId) {
    try {
        state.projects = await requestJson("/api/query/project-instances");
        renderProjects();
        renderLaunchProjectControls(preferredProjectId);
        const nextProjectId = preferredProjectId && state.projects.some((x) => x.projectId === preferredProjectId)
            ? preferredProjectId
            : state.projects[0]?.projectId;

        if (nextProjectId) {
            await previewProject(nextProjectId);
        } else {
            resetProjectView();
        }
    } catch (error) {
        showToast(error.message);
    }
}

async function previewProject(projectId) {
    if (!projectId) {
        return;
    }

    state.launchProjectId = projectId;
    renderProjects();
    renderLaunchProjectControls(projectId);

    try {
        const [overview, entities, projectDiseaseStats] = await Promise.all([
            requestJson(`/api/query/projects/${projectId}/overview`),
            requestJson(`/api/query/projects/${projectId}/entities`),
            requestJson(`/api/query/projects/${projectId}/disease-statistics`),
        ]);

        state.overview = overview;
        state.entities = entities;
        state.projectDiseaseStats = projectDiseaseStats;
        renderLaunchSummary();
        renderProjectMap();
    } catch (error) {
        showToast(error.message);
    }
}

async function openProjectDetail(projectId) {
    if (!projectId) {
        showToast("请先选择工程期次");
        return;
    }

    showDetail();
    await selectProject(projectId);
}

async function selectProject(projectId) {
    showDetail();
    const loadId = ++state.projectLoadId;
    state.projectId = projectId;
    state.entityId = null;
    renderProjects();

    try {
        const [overview, entities, projectDiseaseStats] = await Promise.all([
            requestJson(`/api/query/projects/${projectId}/overview`),
            requestJson(`/api/query/projects/${projectId}/entities`),
            requestJson(`/api/query/projects/${projectId}/disease-statistics`),
        ]);

        if (loadId !== state.projectLoadId || state.projectId !== projectId) {
            return;
        }

        state.overview = overview;
        state.entities = entities;
        state.projectDiseaseStats = projectDiseaseStats;
        renderSummary();
        renderEntities();
        renderProjectMap();
        renderDiseaseTypeFilter(projectDiseaseStats);

        const pending = state.pendingEntityId && entities.find((x) => x.entityId === state.pendingEntityId);
        state.pendingEntityId = null;
        const uploaded = pending || entities.find((x) => x.hasUploadedData) || entities[0];
        if (uploaded) {
            await selectEntity(uploaded.entityId);
        } else {
            setStatsScope("project");
        }
    } catch (error) {
        if (loadId !== state.projectLoadId || state.projectId !== projectId) {
            return;
        }

        showToast(error.message);
    }
}

async function selectEntity(entityId) {
    state.entityId = entityId;
    state.imageIndex = 0;
    state.selectedDiseaseId = null;
    state.diseasePreviewMode = "exact";
    state.pointCloudFrames = [];
    state.pointCloudIndex = 0;
    renderEntities();

    try {
        const [grayImages, diseaseImages, entityDiseaseStats] = await Promise.all([
            requestJson(`/api/query/projects/${state.projectId}/entities/${entityId}/gray-images`),
            requestJson(`/api/query/projects/${state.projectId}/entities/${entityId}/disease-images`),
            requestJson(`/api/query/projects/${state.projectId}/disease-statistics?entityId=${entityId}`),
        ]);

        state.grayImages = grayImages;
        state.diseaseImages = diseaseImages;
        state.entityDiseaseStats = entityDiseaseStats;
        renderDiseaseTypeFilter(entityDiseaseStats.length ? entityDiseaseStats : state.projectDiseaseStats);
        setStatsScope("entity");
        await loadDiseases();
        await loadRingsForCurrentImage();
        await loadPointCloudFrames();
        renderImageViewer();
    } catch (error) {
        state.grayImages = [];
        state.diseaseImages = [];
        state.ringLocations = [];
        state.diseases = [];
        state.entityDiseaseStats = [];
        state.pointCloudFrames = [];
        renderImageViewer();
        renderDiseaseRows();
        renderPointCloudFrames();
        showToast(error.message);
    }
}

async function loadDiseases() {
    if (!state.projectId || !state.entityId) {
        return;
    }

    const params = new URLSearchParams({
        ProjectInstanceId: state.projectId,
        EntityId: state.entityId,
        PageSize: "300",
    });

    const diseaseType = $("diseaseTypeFilter").value;
    const mileageStart = $("mileageStart").value;
    const mileageEnd = $("mileageEnd").value;

    if (diseaseType) params.set("DiseaseType", diseaseType);
    if (mileageStart) params.set("MileageStart", mileageStart);
    if (mileageEnd) params.set("MileageEnd", mileageEnd);

    try {
        const result = await requestJson(`/api/diseases/query?${params.toString()}`);
        state.diseases = result.items || [];
        if (state.selectedDiseaseId && !state.diseases.some((item) => item.diseaseId === state.selectedDiseaseId)) {
            state.selectedDiseaseId = null;
            state.diseasePreviewMode = "exact";
        }
        $("diseaseMeta").textContent = `显示 ${state.diseases.length} / ${result.totalCount} 条，双击查看高清图`;
        renderDiseaseRows();
        renderDiseaseOverlay();
    } catch (error) {
        showToast(error.message);
    }
}

async function loadRingsForCurrentImage() {
    const image = state.grayImages[state.imageIndex];
    if (!image) {
        state.ringLocations = [];
        state.ringPreviewMode = "mileage";
        return;
    }

    const params = new URLSearchParams();
    if (image.beginMileage !== null && image.beginMileage !== undefined) params.set("mileageStart", image.beginMileage);
    if (image.endMileage !== null && image.endMileage !== undefined) params.set("mileageEnd", image.endMileage);

    const url = `/api/query/projects/${state.projectId}/entities/${state.entityId}/ring-locations`;
    let rings = await requestJson(`${url}?${params.toString()}`);
    state.ringPreviewMode = "mileage";

    if (rings.length === 0) {
        rings = await requestJson(url);
        state.ringPreviewMode = rings.length > 0 ? "sequence" : "mileage";
    }

    state.ringLocations = rings;
}

async function loadPointCloudFrames() {
    if (!state.projectId || !state.entityId) {
        state.pointCloudFrames = [];
        renderPointCloudFrames();
        return;
    }

    try {
        const tree = await requestJson(`/api/projects/${state.projectId}/entities/${state.entityId}/file-tree`);
        const cloudNode = findNodeByName(tree, "04点云");
        state.pointCloudFrames = cloudNode ? flattenFiles(cloudNode).sort(compareFrameName) : [];
    } catch {
        state.pointCloudFrames = [];
    }

    state.pointCloudIndex = 0;
    renderPointCloudFrames();
}

function findNodeByName(node, name) {
    if (!node) {
        return null;
    }

    if (node.name === name) {
        return node;
    }

    for (const child of node.children || []) {
        const found = findNodeByName(child, name);
        if (found) {
            return found;
        }
    }

    return null;
}

function flattenFiles(node) {
    if (!node.children?.length) {
        return node.isDirectory ? [] : [node];
    }

    return node.children.flatMap(flattenFiles);
}

function compareFrameName(a, b) {
    const left = extractFrameNumber(a.name);
    const right = extractFrameNumber(b.name);
    return left - right || a.name.localeCompare(b.name, "zh-CN");
}

function extractFrameNumber(name) {
    const match = String(name || "").match(/\d+/);
    return match ? Number(match[0]) : Number.MAX_SAFE_INTEGER;
}

async function selectImage(index) {
    if (!state.grayImages.length) {
        return;
    }

    state.imageIndex = (index + state.grayImages.length) % state.grayImages.length;
    state.diseasePreviewMode = "exact";
    scrollToImage(state.imageIndex, true);
    await loadRingsForCurrentImage();
    renderImageProgress();
    renderRingOverlay();
    renderDiseaseOverlay();
    updateViewerMeta();
}

function selectPointCloudFrame(index) {
    state.pointCloudIndex = Math.max(0, Math.min(index, state.pointCloudFrames.length - 1));
    renderPointCloudFrames();
}

function setStatsScope(scope) {
    state.activeStatsScope = scope;
    $("entityStatsButton").classList.toggle("active", scope === "entity");
    $("projectStatsButton").classList.toggle("active", scope === "project");
    renderDiseaseStats();
}

function renderProjects() {
    const select = $("projectSelect");
    $("projectCount").textContent = state.projects.length;
    $("projectInstanceCount").textContent = state.projects.length;
    select.innerHTML = state.projects
        .map((project) => {
            const selected = project.projectId === state.projectId ? " selected" : "";
            return `<option value="${project.projectId}"${selected}>${escapeHtml(project.displayName)}</option>`;
        })
        .join("");

    if (state.projectId && state.projects.some((project) => project.projectId === state.projectId)) {
        select.value = state.projectId;
    }
}

function renderLaunchProjectControls(preferredProjectId) {
    const lineSelect = $("lineSelect");
    const dateSelect = $("dateSelect");
    const groups = groupProjectsByLine();
    const selected = state.projects.find((item) => item.projectId === (preferredProjectId || state.launchProjectId))
        || state.projects[0];
    const selectedLine = selected ? buildLineKey(selected) : "";

    lineSelect.innerHTML = groups.map((group) => `
        <option value="${escapeAttr(group.key)}"${group.key === selectedLine ? " selected" : ""}>${escapeHtml(group.label)}</option>
    `).join("");

    const currentGroup = groups.find((group) => group.key === (lineSelect.value || selectedLine)) || groups[0];
    const projectOptions = currentGroup?.projects || [];
    dateSelect.innerHTML = projectOptions.map((project) => `
        <option value="${project.projectId}"${project.projectId === selected?.projectId ? " selected" : ""}>${project.collectionDate} · ${escapeHtml(project.direction)}</option>
    `).join("");

    if (preferredProjectId && projectOptions.some((project) => project.projectId === preferredProjectId)) {
        dateSelect.value = preferredProjectId;
    }

    $("projectCards").innerHTML = state.projects.map((project) => `
        <button class="project-card ${project.projectId === state.launchProjectId ? "active" : ""}" data-launch-project-id="${project.projectId}">
            <strong>${escapeHtml(project.projectName)}</strong>
            <span>${escapeHtml(project.direction)} · ${project.collectionDate} · ${project.uploadedEntityCount}/${project.entityCount} 已上传</span>
        </button>
    `).join("");

    document.querySelectorAll("[data-launch-project-id]").forEach((button) => {
        button.addEventListener("click", () => previewProject(button.dataset.launchProjectId));
        button.addEventListener("dblclick", () => openProjectDetail(button.dataset.launchProjectId));
    });
}

function handleLineSelectChange() {
    const groups = groupProjectsByLine();
    const group = groups.find((item) => item.key === $("lineSelect").value);
    const projectId = group?.projects[0]?.projectId;
    renderLaunchProjectControls(projectId);
    if (projectId) {
        previewProject(projectId);
    }
}

function groupProjectsByLine() {
    const map = new Map();
    for (const project of state.projects) {
        const key = buildLineKey(project);
        if (!map.has(key)) {
            map.set(key, {
                key,
                label: `${project.projectName} · ${project.direction}`,
                projects: [],
            });
        }

        map.get(key).projects.push(project);
    }

    return [...map.values()]
        .map((group) => ({
            ...group,
            projects: group.projects.sort((a, b) => String(b.collectionDate).localeCompare(String(a.collectionDate))),
        }))
        .sort((a, b) => a.label.localeCompare(b.label, "zh-CN"));
}

function buildLineKey(project) {
    return `${project.projectName}|${project.direction}`;
}

function renderLaunchSummary() {
    const overview = state.overview;
    if (!overview) {
        $("launchSummary").innerHTML = "";
        $("entityCountLaunch").textContent = "0";
        return;
    }

    $("entityCountLaunch").textContent = overview.project.entityCount;
    $("launchMapTitle").textContent = overview.project.displayName || overview.project.projectName;
    const stats = [
        ["站点区间", overview.project.entityCount],
        ["已上传", overview.project.uploadedEntityCount],
        ["病害", overview.diseaseCount],
        ["灰度图", overview.grayImageCount],
        ["环片", overview.ringCount],
        ["点云", overview.pointCloudFileCount],
    ];

    $("launchSummary").innerHTML = `
        <div class="launch-line-title">${escapeHtml(overview.project.projectName)}</div>
        <div class="item-meta">${escapeHtml(overview.project.direction)} · ${overview.project.collectionDate} · ${formatNumber(overview.mileageRange.minMileage, 0)}-${formatNumber(overview.mileageRange.maxMileage, 0)}</div>
        <div class="launch-metrics">
            ${stats.map(([label, value]) => `<div><span>${label}</span><strong>${value}</strong></div>`).join("")}
        </div>
    `;
}

function renderEntities() {
    $("entityCount").textContent = state.entities.length;
    $("entityList").innerHTML = state.entities.map((entity) => `
        <button class="entity-item ${entity.entityId === state.entityId ? "active" : ""}" data-entity-id="${entity.entityId}">
            <div class="item-title">${escapeHtml(entity.displayName)}</div>
            <div class="item-meta">${entity.stationType === 0 ? "站点" : "区间"} · ${formatNumber(entity.beginMileage, 3)}-${formatNumber(entity.endMileage, 3)} · 病害 ${entity.diseaseCount}</div>
        </button>
    `).join("");

    document.querySelectorAll("[data-entity-id]").forEach((button) => {
        button.addEventListener("click", () => selectEntity(button.dataset.entityId));
    });
}

function initializeProjectMap() {
    if (!window.T || !$("projectMap")) {
        $("mapMeta").textContent = "地图脚本未加载，仍可通过左侧列表查看站点";
        return;
    }

    state.map = new T.Map("projectMap");
    state.map.centerAndZoom(new T.LngLat(116.1705, 39.924), 14);
    state.map.enableScrollWheelZoom();
    state.mapReady = true;
}

function renderProjectMap() {
    if (!$("mapMeta") || !$("mapLegend")) {
        return;
    }

    $("mapLegend").innerHTML = `
        <span>站点</span>
        <span>区间连线</span>
    `;

    if (!state.mapReady || !state.map) {
        $("mapMeta").textContent = "地图暂不可用";
        return;
    }

    state.map.clearOverLays();
    state.mapOverlays = [];

    const entitiesWithPoints = state.entities
        .map((entity) => ({
            entity,
            begin: parseGps(entity.beginGps),
            end: parseGps(entity.endGps),
        }))
        .filter((item) => item.begin || item.end);

    if (entitiesWithPoints.length === 0) {
        $("mapMeta").textContent = "当前工程暂无 GPS 坐标";
        state.map.centerAndZoom(new T.LngLat(116.1705, 39.924), 13);
        return;
    }

    const allPoints = [];
    const stationMap = new Map();

    for (const item of entitiesWithPoints) {
        if (item.begin && item.end && (item.begin.lng !== item.end.lng || item.begin.lat !== item.end.lat)) {
            const line = new T.Polyline(
                [new T.LngLat(item.begin.lng, item.begin.lat), new T.LngLat(item.end.lng, item.end.lat)],
                { color: "#27b7d6", weight: 5, opacity: 0.76, lineStyle: "solid" });
            state.map.addOverLay(line);
            state.mapOverlays.push(line);
        }

        addStationPoint(stationMap, item.entity.beginStation, item.begin, item.entity);
        addStationPoint(stationMap, item.entity.endStation, item.end, item.entity);
    }

    for (const station of stationMap.values()) {
        const point = new T.LngLat(station.lng, station.lat);
        const marker = new T.Marker(point);
        marker.addEventListener("click", () => {
            const entity = station.entities.find((x) => x.hasUploadedData) || station.entities[0];
            if (entity) {
                state.pendingEntityId = entity.entityId;
                openProjectDetail(state.launchProjectId || state.projectId);
            }
            marker.openInfoWindow(new T.InfoWindow(buildMapPopup(station), { offset: new T.Point(0, -24) }));
        });
        state.map.addOverLay(marker);
        state.mapOverlays.push(marker);
        allPoints.push(point);
    }

    if (allPoints.length === 1) {
        state.map.centerAndZoom(allPoints[0], 15);
    } else {
        state.map.setViewport(allPoints);
    }

    const project = state.projects.find((item) => item.projectId === (state.launchProjectId || state.projectId));
    $("mapMeta").textContent = `${project?.displayName || "当前工程"} · ${stationMap.size} 个站点 · ${state.entities.length} 个实体`;
}

function addStationPoint(stationMap, name, gps, entity) {
    if (!gps) {
        return;
    }

    const key = `${name || entity.displayName}|${gps.lng.toFixed(6)},${gps.lat.toFixed(6)}`;
    const existing = stationMap.get(key);
    if (existing) {
        existing.entities.push(entity);
        return;
    }

    stationMap.set(key, {
        name: name || entity.displayName,
        lng: gps.lng,
        lat: gps.lat,
        entities: [entity],
    });
}

function buildMapPopup(station) {
    const first = station.entities[0];
    return `
        <div class="map-popup">
            <strong>${escapeHtml(station.name)}</strong>
            <div>${station.lng.toFixed(6)}, ${station.lat.toFixed(6)}</div>
            <div>${station.entities.length} 个关联站点/区间</div>
            <div>${escapeHtml(first?.displayName || "")}</div>
        </div>
    `;
}

function parseGps(value) {
    const match = String(value || "").match(/(-?\d+(?:\.\d+)?)\s*[,，]\s*(-?\d+(?:\.\d+)?)/);
    if (!match) {
        return null;
    }

    const lng = Number(match[1]);
    const lat = Number(match[2]);
    return Number.isFinite(lng) && Number.isFinite(lat) ? { lng, lat } : null;
}

function renderSummary() {
    const overview = state.overview;
    $("projectInfo").innerHTML = `
        <div class="item-title">${escapeHtml(overview.project.projectName)}</div>
        <div class="item-meta">${escapeHtml(overview.project.direction)} · ${overview.project.collectionDate} · ${overview.project.uploadedEntityCount}/${overview.project.entityCount} 已上传</div>
    `;

    const metrics = [
        ["里程", `${formatNumber(overview.mileageRange.minMileage, 0)}-${formatNumber(overview.mileageRange.maxMileage, 0)}`],
        ["区间", overview.project.entityCount],
        ["上传", overview.project.uploadedEntityCount],
        ["病害", overview.diseaseCount],
        ["灰度图", overview.grayImageCount],
        ["点云", overview.pointCloudFileCount],
    ];

    $("summaryStrip").innerHTML = metrics.map(([label, value]) => `
        <div class="metric">
            <div class="label">${label}</div>
            <div class="value">${value}</div>
        </div>
    `).join("");
}

function renderDiseaseStats() {
    const stats = state.activeStatsScope === "entity" ? state.entityDiseaseStats : state.projectDiseaseStats;
    const total = stats.reduce((sum, item) => sum + item.count, 0);
    const max = Math.max(1, ...stats.map((x) => x.count));
    $("donutTotal").textContent = total;
    $("donutScope").textContent = state.activeStatsScope === "entity" ? "当前区间" : "当前工程";
    renderDonut(stats);

    $("diseaseStats").innerHTML = stats.length
        ? stats.map((item) => `
            <div class="bar-row" title="${escapeHtml(item.diseaseType)} ${item.count}">
                <div class="bar-label">${escapeHtml(item.diseaseType)}</div>
                <div class="bar-track"><div class="bar-fill" style="width:${Math.max(4, item.count / max * 100)}%"></div></div>
                <div>${item.count}</div>
            </div>
        `).join("")
        : `<div class="empty-list">暂无病害统计</div>`;
}

function renderDonut(stats) {
    const donut = $("diseaseDonut");
    const total = stats.reduce((sum, item) => sum + item.count, 0);
    if (total === 0) {
        donut.style.background = "conic-gradient(var(--line) 0deg 360deg)";
        return;
    }

    const colors = ["var(--track)", "var(--green)", "var(--signal)", "var(--amber)", "#7bdff2"];
    let current = 0;
    const parts = stats.map((item, index) => {
        const start = current;
        current += item.count / total * 360;
        return `${colors[index % colors.length]} ${start}deg ${current}deg`;
    });
    donut.style.background = `conic-gradient(${parts.join(", ")})`;
}

function renderDiseaseTypeFilter(stats) {
    const currentValue = $("diseaseTypeFilter").value;
    $("diseaseTypeFilter").innerHTML = `<option value="">全部病害</option>` + stats
        .map((item) => `<option value="${escapeAttr(item.diseaseType)}">${escapeHtml(item.diseaseType)}</option>`)
        .join("");

    if ([...$("diseaseTypeFilter").options].some((option) => option.value === currentValue)) {
        $("diseaseTypeFilter").value = currentValue;
    }
}

function renderImageViewer() {
    const image = state.grayImages[state.imageIndex];
    const strip = $("imageStrip");
    $("imageEmpty").style.display = image ? "none" : "block";
    strip.style.display = image ? "flex" : "none";
    strip.innerHTML = state.grayImages.map((item, index) => `
        <img class="strip-image" src="${escapeAttr(item.fileUrl)}" alt="${escapeAttr(item.fileName)}" data-strip-index="${index}">
    `).join("");
    $("thumbnailStrip").innerHTML = state.grayImages.map((item, index) => `
        <img class="thumbnail-segment" src="${escapeAttr(item.fileUrl)}" alt="" data-thumbnail-index="${index}">
    `).join("");

    window.requestAnimationFrame(() => {
        scrollToImage(state.imageIndex, false);
        renderImageProgress();
    });

    renderRingOverlay();
    renderDiseaseOverlay();
    updateViewerMeta();
}

function scrollToImage(index, smooth) {
    const stage = $("imageStage");
    const item = document.querySelector(`[data-strip-index="${index}"]`);
    if (!stage || !item) {
        return;
    }

    const targetLeft = item.offsetLeft - Math.max(0, (stage.clientWidth - item.clientWidth) / 2);
    stage.scrollTo({ left: Math.max(0, targetLeft), behavior: smooth ? "smooth" : "auto" });
}

function syncImageIndexFromScroll() {
    window.clearTimeout(state.imageScrollTimer);
    renderImageProgress();
    state.imageScrollTimer = window.setTimeout(async () => {
        const nextIndex = getImageIndexAtViewportCenter();
        if (nextIndex === state.imageIndex || nextIndex < 0) {
            return;
        }

        state.imageIndex = nextIndex;
        state.diseasePreviewMode = "exact";
        await loadRingsForCurrentImage();
        renderImageProgress();
        renderRingOverlay();
        renderDiseaseOverlay();
        updateViewerMeta();
    }, 90);
}

function getImageIndexAtViewportCenter() {
    const stage = $("imageStage");
    const images = [...document.querySelectorAll("[data-strip-index]")];
    if (!stage || images.length === 0) {
        return -1;
    }

    const center = stage.scrollLeft + stage.clientWidth / 2;
    let bestIndex = 0;
    let bestDistance = Number.POSITIVE_INFINITY;
    images.forEach((item) => {
        const itemCenter = item.offsetLeft + item.clientWidth / 2;
        const distance = Math.abs(itemCenter - center);
        if (distance < bestDistance) {
            bestDistance = distance;
            bestIndex = Number(item.dataset.stripIndex);
        }
    });

    return bestIndex;
}

function renderImageProgress() {
    const stage = $("imageStage");
    const frame = $("thumbnailActiveFrame");
    const totalWidth = Math.max(1, stage?.scrollWidth || 1);
    const visibleWidth = Math.min(totalWidth, stage?.clientWidth || totalWidth);
    const leftRatio = stage ? Math.min(1, Math.max(0, stage.scrollLeft / totalWidth)) : 0;
    const widthRatio = Math.min(1, Math.max(0.04, visibleWidth / totalWidth));

    frame.style.left = `${leftRatio * 100}%`;
    frame.style.width = `${widthRatio * 100}%`;
}

function handleProgressHover(event) {
    if (!state.grayImages.length) {
        return;
    }

    const { ratio, x } = getProgressPointer(event);
    const index = getImageIndexByRatio(ratio);
    const image = state.grayImages[index];
    const preview = $("imageProgressPreview");
    $("imageProgressPreviewImg").src = image.fileUrl;
    $("imageProgressPreviewText").textContent = `${image.fileName} · ${formatNumber(image.beginMileage, 1)}-${formatNumber(image.endMileage, 1)}`;
    preview.style.left = `${x}px`;
    preview.classList.add("show");
}

function hideProgressPreview() {
    $("imageProgressPreview").classList.remove("show");
}

function handleProgressClick(event) {
    if (!state.grayImages.length) {
        return;
    }

    const { ratio } = getProgressPointer(event);
    const index = getImageIndexByRatio(ratio);
    selectImage(index);
}

function getProgressPointer(event) {
    const rect = $("thumbnailProgress").getBoundingClientRect();
    const x = Math.min(rect.width, Math.max(0, event.clientX - rect.left));
    return {
        x,
        ratio: rect.width <= 0 ? 0 : x / rect.width,
    };
}

function getImageIndexByRatio(ratio) {
    const index = Math.round(Math.min(1, Math.max(0, ratio)) * (state.grayImages.length - 1));
    return Math.max(0, Math.min(state.grayImages.length - 1, index));
}

function renderRingOverlay() {
    const overlay = $("ringOverlay");
    const notice = $("ringNotice");
    const image = state.grayImages[state.imageIndex];
    notice.style.display = "none";
    notice.textContent = "";

    if (!$("ringToggle").checked || !image || image.beginMileage === null || image.endMileage === null) {
        overlay.innerHTML = "";
        return;
    }

    const rings = state.ringLocations
        .filter((ring) => ring.beginMileage !== null || ring.endMileage !== null)
        .slice(0, 180);

    if (rings.length === 0) {
        overlay.innerHTML = "";
        return;
    }

    const begin = image.beginMileage;
    const end = image.endMileage;
    const width = Math.max(0.001, end - begin);

    if (state.ringPreviewMode === "sequence") {
        notice.style.display = "block";
        notice.textContent = "样例图里程与环片里程未对齐，当前按环序均匀预览。正式数据会按真实里程叠加。";
    }

    overlay.innerHTML = rings.map((ring, index) => {
        const left = state.ringPreviewMode === "sequence"
            ? (rings.length === 1 ? 50 : index / (rings.length - 1) * 100)
            : Math.min(100, Math.max(0, (((ring.beginMileage ?? ring.endMileage) - begin) / width) * 100));
        const label = ring.sourceRingId ?? index + 1;
        return `<div class="ring-mark" style="left:${left}%"></div><div class="ring-label" style="left:${left}%">${label}</div>`;
    }).join("");
}

function renderDiseaseRows() {
    $("diseaseRows").innerHTML = state.diseases.length
        ? state.diseases.map((disease) => `
            <tr class="${disease.diseaseId === state.selectedDiseaseId ? "selected" : ""}" data-disease-id="${disease.diseaseId}">
                <td>${escapeHtml(disease.diseaseType || "未分类")}</td>
                <td>${formatNumber(disease.mileage, 3)}</td>
                <td>${formatNumber(disease.beginMileage, 2)}-${formatNumber(disease.endMileage, 2)}</td>
                <td>${escapeHtml(disease.imageName || "")}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="4">暂无病害记录</td></tr>`;

    document.querySelectorAll("[data-disease-id]").forEach((row) => {
        row.addEventListener("click", () => focusDiseaseOnImage(row.dataset.diseaseId));
        row.addEventListener("dblclick", () => openDiseaseImage(row.dataset.diseaseId));
    });
}

async function focusDiseaseOnImage(diseaseId) {
    const disease = state.diseases.find((item) => item.diseaseId === diseaseId);
    if (!disease) {
        return;
    }

    state.selectedDiseaseId = diseaseId;
    setActiveView("image");

    const match = findBestGrayImageForDisease(disease);
    state.diseasePreviewMode = match?.exact ? "exact" : "nearest";

    if (match && match.index !== state.imageIndex) {
        state.imageIndex = match.index;
        await loadRingsForCurrentImage();
        renderImageViewer();
    } else {
        renderDiseaseRows();
        renderDiseaseOverlay();
        updateViewerMeta();
    }

    showToast(state.diseasePreviewMode === "exact"
        ? "已跳转到病害所在灰度图，并按里程标记位置"
        : "样例数据里程未完全对齐，已跳到最近灰度图做预览标记");
}

function findBestGrayImageForDisease(disease) {
    if (!state.grayImages.length) {
        return null;
    }

    const mileage = getDiseaseMileage(disease);
    if (mileage === null) {
        return { index: state.imageIndex, exact: false };
    }

    const exactIndex = state.grayImages.findIndex((image) => isMileageInsideImage(mileage, image));
    if (exactIndex >= 0) {
        return { index: exactIndex, exact: true };
    }

    let nearestIndex = 0;
    let nearestDistance = Number.POSITIVE_INFINITY;
    state.grayImages.forEach((image, index) => {
        const distance = distanceToImageMileage(mileage, image);
        if (distance < nearestDistance) {
            nearestDistance = distance;
            nearestIndex = index;
        }
    });

    return { index: nearestIndex, exact: false };
}

function renderDiseaseOverlay() {
    const overlay = $("diseaseOverlay");
    const notice = $("ringNotice");
    const image = state.grayImages[state.imageIndex];

    if (!overlay || !image) {
        if (overlay) overlay.innerHTML = "";
        return;
    }

    const selected = state.diseases.find((item) => item.diseaseId === state.selectedDiseaseId);
    const visibleDiseases = state.diseases
        .map((disease) => ({ disease, position: getDiseasePositionOnImage(disease, image) }))
        .filter((item) => item.position !== null && (item.position.inRange || item.disease.diseaseId === state.selectedDiseaseId))
        .slice(0, 120);

    overlay.innerHTML = visibleDiseases.map(({ disease, position }) => {
        const selectedClass = disease.diseaseId === state.selectedDiseaseId ? " selected" : "";
        const previewClass = position.inRange ? "" : " preview";
        const left = Math.min(98, Math.max(2, position.left));
        const top = 50 + ((extractFrameNumber(disease.diseaseId) % 7) - 3) * 4;
        const label = escapeAttr(disease.diseaseType || "病害");
        return `
            <button class="disease-marker${selectedClass}${previewClass}" style="left:${left}%;top:${top}%" title="${label} ${formatNumber(getDiseaseMileage(disease), 3)}" type="button"></button>
            ${disease.diseaseId === state.selectedDiseaseId ? `<div class="disease-callout${previewClass}" style="left:${left}%;top:${Math.max(8, top - 13)}%">${label}<span>${formatNumber(getDiseaseMileage(disease), 3)}</span></div>` : ""}
        `;
    }).join("");

    const selectedPosition = selected ? getDiseasePositionOnImage(selected, image) : null;
    if (selected && (state.diseasePreviewMode === "nearest" || selectedPosition?.inRange === false)) {
        notice.style.display = "block";
        notice.textContent = "样例病害里程与灰度图里程未完全对齐，当前按最近灰度图显示预览标记。正式数据里程一致后会按真实位置叠加。";
    }
}

function getDiseasePositionOnImage(disease, image) {
    const mileage = getDiseaseMileage(disease);
    if (mileage === null || image.beginMileage === null || image.beginMileage === undefined || image.endMileage === null || image.endMileage === undefined) {
        return null;
    }

    const begin = Number(image.beginMileage);
    const end = Number(image.endMileage);
    const width = Math.max(0.001, end - begin);
    const rawLeft = (mileage - begin) / width * 100;

    return {
        left: rawLeft,
        inRange: rawLeft >= 0 && rawLeft <= 100,
    };
}

function getDiseaseMileage(disease) {
    const value = disease?.mileage ?? disease?.beginMileage ?? disease?.endMileage;
    const number = Number(value);
    return Number.isFinite(number) ? number : null;
}

function isMileageInsideImage(mileage, image) {
    if (image.beginMileage === null || image.beginMileage === undefined || image.endMileage === null || image.endMileage === undefined) {
        return false;
    }

    return mileage >= Number(image.beginMileage) && mileage <= Number(image.endMileage);
}

function distanceToImageMileage(mileage, image) {
    if (image.beginMileage === null || image.beginMileage === undefined || image.endMileage === null || image.endMileage === undefined) {
        return Number.POSITIVE_INFINITY;
    }

    const begin = Number(image.beginMileage);
    const end = Number(image.endMileage);
    if (mileage < begin) return begin - mileage;
    if (mileage > end) return mileage - end;
    return 0;
}

function renderPointCloudFrames() {
    const slider = $("pointCloudSlider");
    slider.max = Math.max(0, state.pointCloudFrames.length - 1);
    slider.value = Math.min(state.pointCloudIndex, Number(slider.max));
    slider.disabled = state.pointCloudFrames.length === 0;

    const frame = state.pointCloudFrames[state.pointCloudIndex];
    $("pointCloudFrameTitle").textContent = frame
        ? `帧 ${extractFrameNumber(frame.name)} · ${frame.name}`
        : "暂无点云帧";

    $("pointCloudFrameMeta").textContent = frame
        ? `第 ${state.pointCloudIndex + 1}/${state.pointCloudFrames.length} 帧，未来可加载抽稀点云并叠加环形断面。`
        : "04 点云暂无帧文件，当前显示预览断面。";

    $("pointCloudList").textContent = frame
        ? `${frame.relativePath} · ${(frame.size / 1024 / 1024).toFixed(2)} MB`
        : "未来可按帧号排序，并接入 Three.js / Potree 做单帧或连续帧点云浏览。";

    renderPointCloudDots();
    updateViewerMeta();
}

function renderPointCloudDots() {
    const container = $("cloudPoints");
    const seed = state.pointCloudIndex + 1;
    const points = [];

    for (let i = 0; i < 170; i += 1) {
        const angle = ((i * 137.508 + seed * 19) % 360) * Math.PI / 180;
        const radius = 25 + pseudoRandom(i + seed * 41) * 29;
        const wobble = Math.sin(i * 0.7 + seed) * 5;
        const x = 50 + Math.cos(angle) * (radius + wobble);
        const y = 50 + Math.sin(angle) * (radius + wobble * 0.8);
        const opacity = 0.42 + pseudoRandom(seed + i * 13) * 0.56;
        points.push(`<span class="cloud-dot" style="left:${x}%;top:${y}%;opacity:${opacity}"></span>`);
    }

    container.innerHTML = points.join("");
}

function pseudoRandom(value) {
    const x = Math.sin(value * 12.9898) * 43758.5453;
    return x - Math.floor(x);
}

function updateViewerMeta() {
    if (state.activeView === "pointcloud") {
        const frame = state.pointCloudFrames[state.pointCloudIndex];
        $("viewerMeta").textContent = frame
            ? `${frame.name} · ${state.pointCloudIndex + 1}/${state.pointCloudFrames.length}`
            : "当前区间暂无 04 点云帧文件";
        return;
    }

    const image = state.grayImages[state.imageIndex];
    $("viewerMeta").textContent = image
        ? `${image.fileName} · ${formatNumber(image.beginMileage, 3)}-${formatNumber(image.endMileage, 3)} · ${state.imageIndex + 1}/${state.grayImages.length}`
        : "当前区间暂无灰度图";
}

async function openDiseaseImage(diseaseId) {
    const disease = state.diseases.find((item) => item.diseaseId === diseaseId);
    try {
        const image = await requestJson(`/api/query/projects/${state.projectId}/entities/${state.entityId}/diseases/${diseaseId}/best-image`);
        showDiseaseDialog(disease, image);
    } catch {
        const fallback = findNearestDiseaseImage(disease);
        if (fallback) {
            showDiseaseDialog(disease, fallback);
            return;
        }

        showToast("当前病害没有匹配到高清图");
    }
}

function findNearestDiseaseImage(disease) {
    if (!disease || !state.diseaseImages.length) {
        return null;
    }

    const mileage = disease.mileage ?? disease.beginMileage ?? disease.endMileage ?? 0;
    return [...state.diseaseImages].sort((a, b) => {
        const da = Math.abs((a.mileage ?? mileage) - mileage);
        const db = Math.abs((b.mileage ?? mileage) - mileage);
        return da - db;
    })[0];
}

function showDiseaseDialog(disease, image) {
    $("dialogTitle").textContent = disease?.diseaseType || image.diseaseType || "病害高清图";
    $("dialogMeta").textContent = `${image.fileName} · 里程 ${formatNumber(image.mileage, 3)}`;
    $("dialogImage").src = image.fileUrl;
    $("imageDialog").showModal();
}

function renderApiCatalog() {
    $("apiStatus").textContent = `${apiCatalog.length} 个`;
    $("apiList").innerHTML = apiCatalog.map(([method, path, note]) => `
        <div class="api-item">
            <div class="api-method">${method}</div>
            <div class="api-path">${path}</div>
            <div class="api-note">${note}</div>
        </div>
    `).join("");
}

function resetProjectView() {
    state.overview = null;
    state.entities = [];
        state.projectDiseaseStats = [];
    state.entityDiseaseStats = [];
    state.diseases = [];
    state.grayImages = [];
    state.ringLocations = [];
    state.pointCloudFrames = [];
    $("projectInfo").innerHTML = "";
    $("summaryStrip").innerHTML = "";
    renderEntities();
    renderProjectMap();
    renderDiseaseStats();
    renderImageViewer();
    renderDiseaseRows();
    renderPointCloudFrames();
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function escapeAttr(value) {
    return escapeHtml(value);
}

initialize();
