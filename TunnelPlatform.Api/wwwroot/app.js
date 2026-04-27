const state = {
    projects: [],
    projectId: null,
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
    projectLoadId: 0,
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
    const response = await fetch(url);
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(error.message || response.statusText);
    }

    return response.json();
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

async function initialize() {
    applyTheme(localStorage.getItem("tunnel-theme") || "dark");
    bindEvents();
    renderApiCatalog();
    await loadProjects();
}

function bindEvents() {
    $("refreshButton").addEventListener("click", () => loadProjects(state.projectId));
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
    $("imageTimeline").style.display = view === "image" ? "flex" : "none";
    $("prevImageButton").style.display = view === "image" ? "inline-flex" : "none";
    $("nextImageButton").style.display = view === "image" ? "inline-flex" : "none";
    $("ringToggle").closest(".switch").style.display = view === "image" ? "inline-flex" : "none";
    $("viewerTitle").textContent = view === "image" ? "二维连续浏览" : "点云断面浏览";
    updateViewerMeta();
}

function handleImageWheel(event) {
    if (state.grayImages.length <= 1) {
        return;
    }

    event.preventDefault();
    if (state.wheelBusy) {
        return;
    }

    state.wheelBusy = true;
    window.setTimeout(() => {
        state.wheelBusy = false;
    }, 180);

    selectImage(state.imageIndex + (event.deltaY > 0 ? 1 : -1));
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
        const nextProjectId = preferredProjectId && state.projects.some((x) => x.projectId === preferredProjectId)
            ? preferredProjectId
            : state.projects[0]?.projectId;

        if (nextProjectId) {
            await selectProject(nextProjectId);
        } else {
            resetProjectView();
        }
    } catch (error) {
        showToast(error.message);
    }
}

async function selectProject(projectId) {
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
        renderDiseaseTypeFilter(projectDiseaseStats);

        const uploaded = entities.find((x) => x.hasUploadedData) || entities[0];
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
    await loadRingsForCurrentImage();
    renderImageViewer();
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
    const img = $("grayImage");
    $("imageEmpty").style.display = image ? "none" : "block";
    img.style.display = image ? "block" : "none";

    if (image) {
        img.src = image.fileUrl;
    } else {
        img.removeAttribute("src");
    }

    $("imageTimeline").innerHTML = state.grayImages.map((item, index) => `
        <div class="tick ${index === state.imageIndex ? "active" : ""}" data-image-index="${index}">
            <strong>${escapeHtml(item.fileName)}</strong>
            <span>${formatNumber(item.beginMileage, 3)}-${formatNumber(item.endMileage, 3)}</span>
        </div>
    `).join("");

    document.querySelectorAll("[data-image-index]").forEach((tick) => {
        tick.addEventListener("click", () => selectImage(Number(tick.dataset.imageIndex)));
    });

    renderRingOverlay();
    renderDiseaseOverlay();
    updateViewerMeta();
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
