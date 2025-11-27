// Modern Stok Takip Sistemi JavaScript Functions

// Global variables
let currentTheme = 'light';
let sidebarCollapsed = false;

// DOM Ready
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
    setupEventListeners();
    setupAnimations();
});

// Initialize Application
function initializeApp() {
    console.log('Stok Takip Sistemi başlatılıyor...');
    
    // Check for saved theme preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        currentTheme = savedTheme;
        applyTheme(currentTheme);
    }
    
    // Check for saved sidebar state
    const savedSidebarState = localStorage.getItem('sidebarCollapsed');
    if (savedSidebarState === 'true') {
        sidebarCollapsed = true;
        toggleSidebar();
    }
    
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize data tables
    initializeDataTables();
    
    // Initialize search functionality
    initializeSearch();
}

// Setup Event Listeners
function setupEventListeners() {
    // Sidebar toggle
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', toggleSidebar);
    }
    
    // Mobile sidebar overlay
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', closeMobileSidebar);
    }
    
    // Form submissions
    setupFormSubmissions();
    
    // Table interactions
    setupTableInteractions();
    
    // Search functionality
    setupSearchFunctionality();
}

// Sidebar Functions
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.getElementById('mainContent');
    
    if (sidebar && mainContent) {
        sidebarCollapsed = !sidebarCollapsed;
        
        if (sidebarCollapsed) {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('expanded');
        } else {
            sidebar.classList.remove('collapsed');
            mainContent.classList.remove('expanded');
        }
        
        // Save state
        localStorage.setItem('sidebarCollapsed', sidebarCollapsed);
        
        // Trigger resize event for charts
        window.dispatchEvent(new Event('resize'));
    }
}

function closeMobileSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    
    if (sidebar && overlay) {
        sidebar.classList.remove('show');
        overlay.classList.remove('show');
    }
}

// Theme Functions
function toggleTheme() {
    currentTheme = currentTheme === 'light' ? 'dark' : 'light';
    applyTheme(currentTheme);
    localStorage.setItem('theme', currentTheme);
}

function applyTheme(theme) {
    const body = document.body;
    
    if (theme === 'dark') {
        body.classList.add('dark-theme');
        body.classList.remove('light-theme');
    } else {
        body.classList.add('light-theme');
        body.classList.remove('dark-theme');
    }
}

// Tooltip Functions
function initializeTooltips() {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
}

// Data Table Functions
function initializeDataTables() {
    const tables = document.querySelectorAll('.modern-table');
    
    tables.forEach(table => {
        // Add sorting functionality
        addTableSorting(table);
        
        // Add row selection
        addTableRowSelection(table);
        
        // Add pagination if needed
        if (table.rows.length > 10) {
            addTablePagination(table);
        }
    });
}

function addTableSorting(table) {
    const headers = table.querySelectorAll('th[data-sortable]');
    
    headers.forEach(header => {
        header.addEventListener('click', function() {
            const column = Array.from(this.parentElement.children).indexOf(this);
            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.children);
            
            // Toggle sort direction
            const isAscending = this.classList.contains('sort-asc');
            
            // Remove existing sort classes
            headers.forEach(h => h.classList.remove('sort-asc', 'sort-desc'));
            
            // Add new sort class
            this.classList.add(isAscending ? 'sort-desc' : 'sort-asc');
            
            // Sort rows
            rows.sort((a, b) => {
                const aValue = a.children[column].textContent.trim();
                const bValue = b.children[column].textContent.trim();
                
                if (isAscending) {
                    return bValue.localeCompare(aValue, 'tr', { numeric: true });
                } else {
                    return aValue.localeCompare(bValue, 'tr', { numeric: true });
                }
            });
            
            // Reorder rows
            rows.forEach(row => tbody.appendChild(row));
        });
    });
}

function addTableRowSelection(table) {
    const rows = table.querySelectorAll('tbody tr');
    
    rows.forEach(row => {
        row.addEventListener('click', function(e) {
            // Don't select if clicking on action buttons
            if (e.target.closest('.action-buttons')) {
                return;
            }
            
            // Toggle selection
            this.classList.toggle('selected');
            
            // Update selection count
            updateSelectionCount();
        });
    });
}

function updateSelectionCount() {
    const selectedRows = document.querySelectorAll('.modern-table tbody tr.selected');
    const countElement = document.getElementById('selectionCount');
    
    if (countElement) {
        countElement.textContent = selectedRows.length;
        countElement.style.display = selectedRows.length > 0 ? 'inline' : 'none';
    }
}

// Search Functions
function initializeSearch() {
    const searchInputs = document.querySelectorAll('.search-input');
    
    searchInputs.forEach(input => {
        // Add search icon
        if (!input.previousElementSibling || !input.previousElementSibling.classList.contains('search-icon')) {
            const icon = document.createElement('div');
            icon.className = 'search-icon';
            icon.innerHTML = '<i class="fas fa-search"></i>';
            input.parentNode.insertBefore(icon, input);
        }
        
        // Add clear button functionality
        setupSearchClearButton(input);
    });
}

function setupTableInteractions() {
    // Table row click handlers
    const tableRows = document.querySelectorAll('.modern-table tbody tr');
    tableRows.forEach(row => {
        row.addEventListener('click', function(e) {
            // Don't trigger if clicking on action buttons
            if (e.target.closest('.btn, .action-buttons')) {
                return;
            }
            
            // Toggle row selection
            this.classList.toggle('selected');
            updateSelectionCount();
        });
    });
    
    // Table header sorting
    const sortableHeaders = document.querySelectorAll('.modern-table th[data-sortable]');
    sortableHeaders.forEach(header => {
        header.addEventListener('click', function() {
            const table = this.closest('table');
            const column = Array.from(this.parentElement.children).indexOf(this);
            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.children);
            
            // Toggle sort direction
            const isAscending = this.classList.contains('sort-asc');
            
            // Remove existing sort classes
            sortableHeaders.forEach(h => h.classList.remove('sort-asc', 'sort-desc'));
            
            // Add new sort class
            this.classList.add(isAscending ? 'sort-desc' : 'sort-asc');
            
            // Sort rows
            rows.sort((a, b) => {
                const aValue = a.children[column].textContent.trim();
                const bValue = b.children[column].textContent.trim();
                
                if (isAscending) {
                    return bValue.localeCompare(aValue, 'tr', { numeric: true });
                } else {
                    return aValue.localeCompare(bValue, 'tr', { numeric: true });
                }
            });
            
            // Reorder rows
            rows.forEach(row => tbody.appendChild(row));
        });
    });
}

function setupSearchFunctionality() {
    const searchForms = document.querySelectorAll('.search-form form');
    
    searchForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const searchInput = this.querySelector('.search-input');
            if (searchInput && searchInput.value.trim() === '') {
                e.preventDefault();
                showNotification('Lütfen arama terimi girin', 'warning');
            }
        });
    });
}

function setupSearchClearButton(input) {
    // Create clear button
    const clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.className = 'search-clear-btn';
    clearBtn.innerHTML = '<i class="fas fa-times"></i>';
    clearBtn.style.display = 'none';
    
    // Insert after input
    input.parentNode.appendChild(clearBtn);
    
    // Show/hide clear button
    input.addEventListener('input', function() {
        clearBtn.style.display = this.value ? 'block' : 'none';
    });
    
    // Clear input
    clearBtn.addEventListener('click', function() {
        input.value = '';
        input.focus();
        this.style.display = 'none';
        
        // Trigger search if auto-search is enabled
        if (input.dataset.autoSearch === 'true') {
            input.form.submit();
        }
    });
}

// Form Functions
function setupFormSubmissions() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        // Add loading state
        form.addEventListener('submit', function() {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner"></span> İşleniyor...';
            }
        });
        
        // Add validation feedback
        const inputs = form.querySelectorAll('.form-control');
        inputs.forEach(input => {
            input.addEventListener('blur', validateInput);
            input.addEventListener('input', clearValidation);
        });
    });
}

function validateInput(e) {
    const input = e.target;
    const value = input.value.trim();
    
    // Remove existing validation classes
    input.classList.remove('is-valid', 'is-invalid');
    
    // Basic validation
    if (input.hasAttribute('required') && !value) {
        input.classList.add('is-invalid');
        showFieldError(input, 'Bu alan zorunludur');
    } else if (input.type === 'email' && value && !isValidEmail(value)) {
        input.classList.add('is-invalid');
        showFieldError(input, 'Geçerli bir e-posta adresi girin');
    } else if (input.classList.contains('is-invalid')) {
        input.classList.add('is-valid');
        clearFieldError(input);
    }
}

function clearValidation(e) {
    const input = e.target;
    input.classList.remove('is-valid', 'is-invalid');
    clearFieldError(input);
}

function showFieldError(input, message) {
    clearFieldError(input);
    
    const errorDiv = document.createElement('div');
    errorDiv.className = 'invalid-feedback';
    errorDiv.textContent = message;
    
    input.parentNode.appendChild(errorDiv);
}

function clearFieldError(input) {
    const errorDiv = input.parentNode.querySelector('.invalid-feedback');
    if (errorDiv) {
        errorDiv.remove();
    }
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Animation Functions
function setupAnimations() {
    // Intersection Observer for fade-in animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    // Observe elements with animation classes
    const animatedElements = document.querySelectorAll('.card, .stat-card, .content-card');
    animatedElements.forEach(el => observer.observe(el));
}

// Utility Functions
function showNotification(message, type = 'info', duration = 5000) {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="fas fa-${getNotificationIcon(type)}"></i>
            <span>${message}</span>
        </div>
        <button class="notification-close">
            <i class="fas fa-times"></i>
        </button>
    `;
    
    // Add to page
    document.body.appendChild(notification);
    
    // Show notification
    setTimeout(() => notification.classList.add('show'), 100);
    
    // Auto hide
    setTimeout(() => hideNotification(notification), duration);
    
    // Close button
    notification.querySelector('.notification-close').addEventListener('click', () => {
        hideNotification(notification);
    });
}

function getNotificationIcon(type) {
    const icons = {
        success: 'check-circle',
        error: 'exclamation-circle',
        warning: 'exclamation-triangle',
        info: 'info-circle'
    };
    return icons[type] || 'info-circle';
}

function hideNotification(notification) {
    notification.classList.remove('show');
    setTimeout(() => notification.remove(), 300);
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Export Functions
function exportTableToExcel(tableId, filename = 'export') {
    const table = document.getElementById(tableId);
    if (!table) return;
    
    // Basic export functionality
    const csvContent = convertTableToCSV(table);
    downloadCSV(csvContent, filename);
}

function convertTableToCSV(table) {
    const rows = table.querySelectorAll('tr');
    let csv = [];
    
    rows.forEach(row => {
        const cols = row.querySelectorAll('td, th');
        const rowData = Array.from(cols).map(col => col.textContent.trim());
        csv.push(rowData.join(','));
    });
    
    return csv.join('\n');
}

function downloadCSV(content, filename) {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `${filename}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Chart Functions
function initializeCharts() {
    // Initialize Chart.js charts if available
    if (typeof Chart !== 'undefined') {
        setupDashboardCharts();
    }
}

function setupDashboardCharts() {
    // Example chart setup
    const ctx = document.getElementById('stockChart');
    if (ctx) {
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran'],
                datasets: [{
                    label: 'Stok Girişi',
                    data: [12, 19, 3, 5, 2, 3],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }
}

// Responsive Functions
function handleResize() {
    const width = window.innerWidth;
    
    // Handle mobile sidebar
    if (width <= 768) {
        const sidebar = document.getElementById('sidebar');
        if (sidebar) {
            sidebar.classList.remove('collapsed');
        }
    }
    
    // Handle table responsiveness
    const tables = document.querySelectorAll('.modern-table');
    tables.forEach(table => {
        if (width <= 768) {
            table.classList.add('mobile-table');
        } else {
            table.classList.remove('mobile-table');
        }
    });
}

// Event Listeners
window.addEventListener('resize', debounce(handleResize, 250));

// Export functions to global scope
window.StokTakip = {
    showNotification,
    exportTableToExcel,
    toggleSidebar,
    toggleTheme,
    debounce,
    throttle
};

console.log('Stok Takip Sistemi JavaScript yüklendi!');