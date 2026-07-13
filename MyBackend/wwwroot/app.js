// API Endpoints
const AUTH_URL = '/api/auth';
const STATS_URL = '/api/stats';

// DOM Elements - Auth Section
const authSection = document.getElementById('auth-section');
const loginForm = document.getElementById('login-form');
const signupForm = document.getElementById('signup-form');
const loginEmail = document.getElementById('login-email');
const loginPassword = document.getElementById('login-password');
const signupUsername = document.getElementById('signup-username');
const signupEmail = document.getElementById('signup-email');
const signupPassword = document.getElementById('signup-password');
const signupConfirmPassword = document.getElementById('signup-confirm-password');
const loginSpinner = document.getElementById('login-spinner');
const signupSpinner = document.getElementById('signup-spinner');

// DOM Elements - Tabs
const tabLogin = document.getElementById('tab-login');
const tabSignup = document.getElementById('tab-signup');

// DOM Elements - Dashboard Section
const dashboardSection = document.getElementById('dashboard-section');
const welcomeMessage = document.getElementById('welcome-message');
const userEmailText = document.getElementById('user-email');
const btnLogout = document.getElementById('btn-logout');
const userAvatarInitials = document.getElementById('user-avatar-initials');

// DOM Elements - KPI Cards
const statTotalUsers = document.getElementById('stat-total-users');
const statActiveSessions = document.getElementById('stat-active-sessions');
const statTodaySignups = document.getElementById('stat-today-signups');
const statActiveRatio = document.getElementById('stat-active-ratio');
const statActiveRatioBar = document.getElementById('stat-active-ratio-bar');
const statDbLatency = document.getElementById('stat-db-latency');

// DOM Elements - Charts & Logs
const signupTrendChart = document.getElementById('signup-trend-chart');
const trendLogRows = document.getElementById('trend-log-rows');

// DOM Elements - Toast Container & Connection Status
const toastContainer = document.getElementById('toast-container');
const connectionBadge = document.getElementById('connection-badge');
const badgeText = document.getElementById('badge-text');

// State
let token = localStorage.getItem('stats_auth_token');
let currentUser = null;
let isConnected = true;

// Initialize Application
document.addEventListener('DOMContentLoaded', () => {
    initApp();
    setupEventListeners();
});

// App Initialization Flow
async function initApp() {
    if (token) {
        setConnectionStatus(true);
        const success = await fetchCurrentUser();
        if (success) {
            showDashboard(true);
            fetchStats();
        } else {
            // Token is expired or invalid
            logoutLocal();
        }
    } else {
        showDashboard(false);
        testApiConnection();
    }
}

// Set up Event Handlers
function setupEventListeners() {
    // Tab switching
    tabLogin.addEventListener('click', () => switchTab('login'));
    tabSignup.addEventListener('click', () => switchTab('signup'));

    // Form Submissions
    loginForm.addEventListener('submit', handleLogin);
    signupForm.addEventListener('submit', handleSignup);

    // Logout Action
    btnLogout.addEventListener('click', handleLogout);

    // Password Visibility Toggles
    setupPasswordToggles();
}

// Test basic connection to API (e.g. checks if swagger or endpoints are reachable)
async function testApiConnection() {
    try {
        const response = await fetch(`${AUTH_URL}/me`, {
            headers: { 'Authorization': `Bearer none` }
        });
        // 401 is expected when not logged in, but means server is running
        if (response.status === 401 || response.ok) {
            setConnectionStatus(true);
        } else {
            setConnectionStatus(false);
        }
    } catch (e) {
        setConnectionStatus(false);
    }
}

// Switch between Login and Signup tabs
function switchTab(mode) {
    if (mode === 'login') {
        tabLogin.classList.add('active');
        tabSignup.classList.remove('active');
        loginForm.classList.remove('hidden');
        signupForm.classList.add('hidden');
    } else {
        tabSignup.classList.add('active');
        tabLogin.classList.remove('active');
        signupForm.classList.remove('hidden');
        loginForm.classList.add('hidden');
    }
}

// Fetch Logged-in User Profile details
async function fetchCurrentUser() {
    try {
        const response = await fetch(`${AUTH_URL}/me`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) return false;

        currentUser = await response.json();
        updateUserProfileUI();
        return true;
    } catch (error) {
        console.error('Error fetching user:', error);
        return false;
    }
}

// Perform User Registration
async function handleSignup(e) {
    e.preventDefault();
    
    const username = signupUsername.value.trim();
    const email = signupEmail.value.trim();
    const password = signupPassword.value;
    const confirm = signupConfirmPassword.value;

    if (password !== confirm) {
        showToast("Passwords do not match", "error");
        return;
    }

    setFormLoading('signup', true);

    try {
        const response = await fetch(`${AUTH_URL}/signup`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Registration failed');
        }

        token = data.token;
        currentUser = data.user;
        localStorage.setItem('stats_auth_token', token);

        showToast("Registration successful!", "success");
        updateUserProfileUI();
        showDashboard(true);
        fetchStats();
        
        // Reset inputs
        signupForm.reset();
    } catch (error) {
        console.error('Signup error:', error);
        showToast(error.message || "Failed to register account", "error");
    } finally {
        setFormLoading('signup', false);
    }
}

// Perform Login Authentication
async function handleLogin(e) {
    e.preventDefault();

    const email = loginEmail.value.trim();
    const password = loginPassword.value;

    setFormLoading('login', true);

    try {
        const response = await fetch(`${AUTH_URL}/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Authentication failed');
        }

        token = data.token;
        currentUser = data.user;
        localStorage.setItem('stats_auth_token', token);

        showToast("Welcome back!", "success");
        updateUserProfileUI();
        showDashboard(true);
        fetchStats();
        
        // Reset inputs
        loginForm.reset();
    } catch (error) {
        console.error('Login error:', error);
        showToast(error.message || "Invalid credentials", "error");
    } finally {
        setFormLoading('login', false);
    }
}

// Perform Log out
async function handleLogout() {
    try {
        await fetch(`${AUTH_URL}/logout`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
    } catch (e) {
        // Suppress network errors on logout
    }

    logoutLocal();
    showToast("Signed out successfully", "success");
}

function logoutLocal() {
    token = null;
    currentUser = null;
    localStorage.removeItem('stats_auth_token');
    showDashboard(false);
    testApiConnection();
}

// Show/Hide Dashboard panel and login card
function showDashboard(loggedIn) {
    if (loggedIn) {
        authSection.classList.add('hidden');
        dashboardSection.classList.remove('hidden');
    } else {
        authSection.classList.remove('hidden');
        dashboardSection.classList.add('hidden');
    }
}

// Set form button loading state spinners
function setFormLoading(formType, isLoading) {
    const btn = formType === 'login' 
        ? loginForm.querySelector('button[type="submit"]') 
        : signupForm.querySelector('button[type="submit"]');
    const spinner = formType === 'login' ? loginSpinner : signupSpinner;

    if (isLoading) {
        btn.disabled = true;
        spinner.classList.remove('hidden');
    } else {
        btn.disabled = false;
        spinner.classList.add('hidden');
    }
}

// Update Profile indicators in welcome header bar
function updateUserProfileUI() {
    if (!currentUser) return;
    welcomeMessage.textContent = `Welcome back, ${currentUser.username}!`;
    userEmailText.textContent = currentUser.email;
    
    // Initials avatar
    const initials = currentUser.username.substring(0, 2).toUpperCase();
    userAvatarInitials.textContent = initials;
}

// Fetch Statistics metrics
async function fetchStats() {
    try {
        const response = await fetch(STATS_URL, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.status === 401) {
            logoutLocal();
            return;
        }

        if (!response.ok) throw new Error("Failed to fetch statistics");

        const stats = await response.json();
        updateStatsUI(stats);
        setConnectionStatus(true);
    } catch (error) {
        console.error('Stats error:', error);
        showToast("Error updating dashboard statistics", "error");
        setConnectionStatus(false);
    }
}

// Render metrics in KPI cards and generate trend visualization
function updateStatsUI(stats) {
    // Metric Texts
    statTotalUsers.textContent = stats.totalUsers;
    statActiveSessions.textContent = stats.activeSessions;
    statTodaySignups.textContent = stats.newSignupsToday;
    statActiveRatio.textContent = `${stats.activePercentage}%`;
    statActiveRatioBar.style.width = `${stats.activePercentage}%`;
    statDbLatency.textContent = `${stats.dbResponseTimeMs.toFixed(2)} ms`;

    // Render Trend Logs Table rows
    trendLogRows.innerHTML = '';
    stats.signupTrend.forEach(item => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${formatDateString(item.date)}</td>
            <td style="font-weight: 600;">${item.count} users</td>
        `;
        trendLogRows.appendChild(row);
    });

    // Render Dynamic SVG Area Chart
    renderSvgTrendChart(stats.signupTrend);
}

// Generate the custom line-and-area SVG trend chart programmatically
function renderSvgTrendChart(trendData) {
    if (!trendData || trendData.length === 0) return;

    const width = 500;
    const height = 220;
    const paddingLeft = 40;
    const paddingRight = 20;
    const paddingTop = 30;
    const paddingBottom = 40;

    const chartWidth = width - paddingLeft - paddingRight;
    const chartHeight = height - paddingTop - paddingBottom;

    // Find min/max values to scale Y axis
    const values = trendData.map(d => d.count);
    const maxVal = Math.max(...values, 5); // default min scale is 5
    const minVal = 0;
    const valRange = maxVal - minVal;

    // Calculate X coordinate intervals
    const pointsCount = trendData.length;
    const xInterval = chartWidth / (pointsCount - 1);

    // Map data elements to coordinate pairs
    const coordinates = trendData.map((d, index) => {
        const x = paddingLeft + (index * xInterval);
        // Invert Y coordinate since SVG (0,0) is top-left
        const ratio = valRange > 0 ? (d.count - minVal) / valRange : 0;
        const y = height - paddingBottom - (ratio * chartHeight);
        return { x, y, date: d.date, count: d.count };
    });

    // Generate line path & area path string
    let linePathStr = '';
    let areaPathStr = '';

    coordinates.forEach((p, idx) => {
        if (idx === 0) {
            linePathStr += `M ${p.x} ${p.y}`;
            areaPathStr += `M ${p.x} ${height - paddingBottom} L ${p.x} ${p.y}`;
        } else {
            linePathStr += ` L ${p.x} ${p.y}`;
            areaPathStr += ` L ${p.x} ${p.y}`;
        }
    });

    areaPathStr += ` L ${coordinates[coordinates.length - 1].x} ${height - paddingBottom} Z`;

    // Construct SVG string
    let svgContent = `
        <svg width="100%" height="100%" viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" style="overflow: visible;">
            <!-- Defs for Gradient Shading -->
            <defs>
                <linearGradient id="chart-gradient" x1="0" y1="0" x2="1" y2="0">
                    <stop offset="0%" stop-color="#6366f1" />
                    <stop offset="50%" stop-color="#3b82f6" />
                    <stop offset="100%" stop-color="#8b5cf6" />
                </linearGradient>
                <linearGradient id="area-gradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stop-color="#6366f1" stop-opacity="0.25" />
                    <stop offset="100%" stop-color="#6366f1" stop-opacity="0.0" />
                </linearGradient>
            </defs>

            <!-- Horizontal Y-Grid lines and Y Labels -->
            <!-- Line 1 (Max/Top) -->
            <line x1="${paddingLeft}" y1="${paddingTop}" x2="${width - paddingRight}" y2="${paddingTop}" class="chart-grid-line" />
            <text x="${paddingLeft - 10}" y="${paddingTop + 3}" class="chart-label" text-anchor="end">${maxVal}</text>

            <!-- Line 2 (Middle Higher) -->
            <line x1="${paddingLeft}" y1="${paddingTop + chartHeight * 0.33}" x2="${width - paddingRight}" y2="${paddingTop + chartHeight * 0.33}" class="chart-grid-line" />
            <text x="${paddingLeft - 10}" y="${paddingTop + chartHeight * 0.33 + 3}" class="chart-label" text-anchor="end">${Math.round(maxVal * 0.67)}</text>

            <!-- Line 3 (Middle Lower) -->
            <line x1="${paddingLeft}" y1="${paddingTop + chartHeight * 0.67}" x2="${width - paddingRight}" y2="${paddingTop + chartHeight * 0.67}" class="chart-grid-line" />
            <text x="${paddingLeft - 10}" y="${paddingTop + chartHeight * 0.67 + 3}" class="chart-label" text-anchor="end">${Math.round(maxVal * 0.33)}</text>

            <!-- Line 4 (Base/Bottom) -->
            <line x1="${paddingLeft}" y1="${height - paddingBottom}" x2="${width - paddingRight}" y2="${height - paddingBottom}" class="chart-axis-line" />
            <text x="${paddingLeft - 10}" y="${height - paddingBottom + 3}" class="chart-label" text-anchor="end">0</text>

            <!-- Fill Area beneath the line -->
            <path d="${areaPathStr}" class="chart-area" />

            <!-- Core Trend line path -->
            <path d="${linePathStr}" class="chart-line" fill="none" />
    `;

    // Add interactive circular points and values
    coordinates.forEach(p => {
        svgContent += `
            <!-- Circular dot point -->
            <circle cx="${p.x}" cy="${p.y}" r="5" class="chart-point" data-date="${p.date}" data-count="${p.count}" />
            <!-- Floating value helper above the point -->
            <text x="${p.x}" y="${p.y - 10}" class="chart-label-value" text-anchor="middle">${p.count}</text>
        `;
    });

    // Add X Axis date labels
    coordinates.forEach(p => {
        const shortDate = formatShortDate(p.date);
        svgContent += `
            <text x="${p.x}" y="${height - paddingBottom + 20}" class="chart-label" text-anchor="middle">${shortDate}</text>
        `;
    });

    svgContent += `</svg>`;
    
    // Inject generated element
    signupTrendChart.innerHTML = svgContent;

    // Attach dynamic click event to dots to show tooltip
    const dots = signupTrendChart.querySelectorAll('.chart-point');
    dots.forEach(dot => {
        dot.addEventListener('click', (e) => {
            const count = e.target.getAttribute('data-count');
            const date = e.target.getAttribute('data-date');
            showToast(`${formatDateString(date)}: ${count} users registered`, 'success');
        });
    });
}

// Date Formatter helpers
function formatShortDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

function formatDateString(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
}

// Display Connection state status badge
function setConnectionStatus(connected) {
    connectionBadge.className = 'badge';
    if (connected) {
        connectionBadge.classList.add('badge-connected');
        badgeText.textContent = 'API Connected';
        isConnected = true;
    } else {
        connectionBadge.classList.add('badge-disconnected');
        badgeText.textContent = 'Connection Error';
        isConnected = false;
    }
}

// Toast Notifications Helper
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    
    const icon = type === 'success' ? 'check-circle' : 'alert-circle';
    toast.innerHTML = `
        <i data-lucide="${icon}"></i>
        <span>${message}</span>
    `;
    
    toastContainer.appendChild(toast);
    lucide.createIcons();

    // Auto animate slide out
    setTimeout(() => {
        toast.style.animation = 'fade-in 0.3s ease reverse forwards';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// Setup password visibility toggles
function setupPasswordToggles() {
    const toggles = document.querySelectorAll('.btn-toggle-password');
    toggles.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = btn.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = btn.querySelector('i, svg');
            
            if (input.type === 'password') {
                input.type = 'text';
                icon.setAttribute('data-lucide', 'eye-off');
            } else {
                input.type = 'password';
                icon.setAttribute('data-lucide', 'eye');
            }
            // Re-render Lucide icons inside this button
            lucide.createIcons();
        });
    });
}
