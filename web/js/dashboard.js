/* ============================================================
   CashFlow Dashboard - Main SPA Application
   Routing, views, charts, CRUD, modals, number formatting
   ============================================================ */

const App = (() => {
  /* ==========================================================
     STATE
     ========================================================== */
  const now = new Date();
  let currentMonth = now.getMonth() + 1;
  let currentYear = now.getFullYear();
  let currentCurrency = 'ARS';
  let currentView = 'dashboard';
  let currentSubView = null;

  let accountsList = [];
  let categoriesList = [];
  let transactionsList = [];
  let exchangeRate = 1;

  let txPage = 1;
  let txPageSize = 50;
  let txTotal = 0;
  let txFilterType = '';
  let txFilterCategory = '';
  let txFilterSearch = '';
  let txFilterAccount = '';
  let txFilterDateFrom = '';
  let txFilterDateTo = '';
  let txDateRangeMode = false;

  let editingTransactionId = null;
  let editingAccountId = null;
  let editingCategoryId = null;

  let chartDonut = null;
  let chartBar = null;

  /* ==========================================================
     MONTH NAMES (Spanish)
     ========================================================== */
  const MONTH_NAMES = [
    'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
    'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
  ];

  /* ==========================================================
     SVG ICONS
     ========================================================== */
  const Icons = {
    dashboard: '<svg viewBox="0 0 24 24"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>',
    accounts: '<svg viewBox="0 0 24 24"><rect x="2" y="5" width="20" height="14" rx="2"/><line x1="2" y1="10" x2="22" y2="10"/></svg>',
    transactions: '<svg viewBox="0 0 24 24"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>',
    import: '<svg viewBox="0 0 24 24"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>',
    history: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
    settings: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>',
    chevronDown: '<svg viewBox="0 0 24 24"><polyline points="6 9 12 15 18 9"/></svg>',
    chevronLeft: '<svg viewBox="0 0 24 24"><polyline points="15 18 9 12 15 6"/></svg>',
    chevronRight: '<svg viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"/></svg>',
    plus: '<svg viewBox="0 0 24 24"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>',
    edit: '<svg viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>',
    trash: '<svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>',
    x: '<svg viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>',
    search: '<svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>',
    arrowDown: '<svg viewBox="0 0 24 24"><line x1="12" y1="5" x2="12" y2="19"/><polyline points="19 12 12 19 5 12"/></svg>',
    arrowUp: '<svg viewBox="0 0 24 24"><line x1="12" y1="19" x2="12" y2="5"/><polyline points="5 12 12 5 19 12"/></svg>',
    arrowLeftRight: '<svg viewBox="0 0 24 24"><polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg>',
    wallet: '<svg viewBox="0 0 24 24"><path d="M20 12V8H6a2 2 0 0 1-2-2c0-1.1.9-2 2-2h12v4"/><path d="M4 6v12c0 1.1.9 2 2 2h14v-4"/><circle cx="18" cy="12" r="1"/></svg>',
    bank: '<svg viewBox="0 0 24 24"><path d="M3 21h18M3 10h18M5 6l7-3 7 3M4 10v11M20 10v11M8 14v3M12 14v3M16 14v3"/></svg>',
    creditCard: '<svg viewBox="0 0 24 24"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>',
    cash: '<svg viewBox="0 0 24 24"><rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M2 10h2M20 10h2M2 14h2M20 14h2"/></svg>',
    pieChart: '<svg viewBox="0 0 24 24"><path d="M21.21 15.89A10 10 0 1 1 8 2.83"/><path d="M22 12A10 10 0 0 0 12 2v10z"/></svg>',
    barChart: '<svg viewBox="0 0 24 24"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>',
    download: '<svg viewBox="0 0 24 24"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>',
    upload: '<svg viewBox="0 0 24 24"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>',
    refresh: '<svg viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/></svg>',
    filter: '<svg viewBox="0 0 24 24"><polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/></svg>',
    tag: '<svg viewBox="0 0 24 24"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>',
    dollar: '<svg viewBox="0 0 24 24"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>',
    exchange: '<svg viewBox="0 0 24 24"><polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg>',
    link: '<svg viewBox="0 0 24 24"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>',
    fields: '<svg viewBox="0 0 24 24"><rect x="3" y="3" width="18" height="18" rx="2"/><line x1="3" y1="9" x2="21" y2="9"/><line x1="3" y1="15" x2="21" y2="15"/><line x1="9" y1="3" x2="9" y2="21"/></svg>',
    audit: '<svg viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>',
    logout: '<svg viewBox="0 0 24 24"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>',
    warning: '<svg viewBox="0 0 24 24"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
    trendUp: '<svg viewBox="0 0 24 24"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/><polyline points="17 6 23 6 23 12"/></svg>',
    trendDown: '<svg viewBox="0 0 24 24"><polyline points="23 18 13.5 8.5 8.5 13.5 1 6"/><polyline points="17 18 23 18 23 12"/></svg>',
    check: '<svg viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>'
  };

  function icon(name) {
    return '<span class="nav-icon">' + (Icons[name] || '') + '</span>';
  }

  function iconRaw(name) {
    return Icons[name] || '';
  }

  /* ==========================================================
     NUMBER FORMATTING (Argentine locale)
     ========================================================== */
  function formatNumber(n) {
    if (n === null || n === undefined) return '0';
    const num = Number(n);
    const parts = Math.abs(num).toFixed(2).split('.');
    const intPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    const decPart = parts[1];
    if (decPart === '00') {
      return (num < 0 ? '-' : '') + intPart;
    }
    return (num < 0 ? '-' : '') + intPart + ',' + decPart;
  }

  function formatCurrency(n, currency) {
    const cur = currency || currentCurrency;
    const prefix = cur === 'USD' ? 'USD ' : '$ ';
    const val = formatNumber(n);
    if (Number(n) < 0) {
      return '-' + prefix + val.replace('-', '');
    }
    return prefix + val;
  }

  function formatCurrencyColored(n, currency) {
    const formatted = formatCurrency(n, currency);
    if (Number(n) < 0) {
      return '<span class="amount-negative">' + formatted + '</span>';
    }
    return '<span class="amount-positive">' + formatted + '</span>';
  }

  /* ==========================================================
     DATE FORMATTING
     ========================================================== */
  function formatDate(dateStr) {
    const d = new Date(dateStr);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return day + '/' + month + '/' + year;
  }

  function formatTime(dateStr) {
    const d = new Date(dateStr);
    return String(d.getHours()).padStart(2, '0') + ':' + String(d.getMinutes()).padStart(2, '0');
  }

  function formatDateTimeLocal(dateStr) {
    if (!dateStr) {
      const now = new Date();
      now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
      return now.toISOString().slice(0, 16);
    }
    const d = new Date(dateStr);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }

  /* ==========================================================
     TOAST NOTIFICATIONS
     ========================================================== */
  function showToast(message, type) {
    let container = document.querySelector('.toast-container');
    if (!container) {
      container = document.createElement('div');
      container.className = 'toast-container';
      document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.className = 'toast toast-' + (type || 'success');
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(() => {
      toast.classList.add('toast-out');
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  }

  /* ==========================================================
     CONFIRM DIALOG
     ========================================================== */
  function showConfirm(title, message, onConfirm) {
    const overlay = document.createElement('div');
    overlay.className = 'confirm-overlay';
    overlay.innerHTML = `
      <div class="confirm-dialog">
        <div class="confirm-icon">${iconRaw('warning')}</div>
        <h3>${title}</h3>
        <p>${message}</p>
        <div class="confirm-actions">
          <button class="btn btn-secondary" data-action="cancel">Cancelar</button>
          <button class="btn btn-danger" data-action="confirm">Eliminar</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
    requestAnimationFrame(() => overlay.classList.add('active'));

    overlay.addEventListener('click', (e) => {
      const action = e.target.dataset.action;
      if (action === 'cancel' || e.target === overlay) {
        overlay.classList.remove('active');
        setTimeout(() => overlay.remove(), 300);
      } else if (action === 'confirm') {
        overlay.classList.remove('active');
        setTimeout(() => overlay.remove(), 300);
        onConfirm();
      }
    });
  }

  /* ==========================================================
     RENDER: FULL APP SHELL
     ========================================================== */
  function renderShell() {
    const user = Auth.getUser();
    const initials = user ? user.username.substring(0, 2).toUpperCase() : 'US';
    const username = user ? user.username : 'Usuario';
    const role = user ? (user.role === 'admin' ? 'Administrador' : 'Usuario') : '';

    document.body.innerHTML = `
    <div class="app-layout">
      <!-- SIDEBAR -->
      <aside class="sidebar" id="sidebar">
        <div class="sidebar-brand">
          <div class="brand-icon">
            <svg viewBox="0 0 24 24" fill="white">
              <line x1="12" y1="1" x2="12" y2="23" stroke="white" stroke-width="2"/>
              <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" stroke="white" stroke-width="2" fill="none"/>
            </svg>
          </div>
          <span class="brand-text">CashFlowApp</span>
        </div>

        <div class="sidebar-toggle" id="sidebarToggle" title="Colapsar menu">
          ${iconRaw('chevronLeft')}
        </div>

        <nav class="sidebar-nav" id="sidebarNav">
          <div class="nav-section-title">Principal</div>
          <div class="nav-item active" data-view="dashboard">
            ${icon('dashboard')}<span class="nav-label">Dashboard</span>
          </div>
          <div class="nav-item" data-view="accounts">
            ${icon('accounts')}<span class="nav-label">Cuentas</span>
          </div>
          <div class="nav-item" data-view="transactions">
            ${icon('transactions')}<span class="nav-label">Movimientos</span>
          </div>
          <div class="nav-item" data-view="import">
            ${icon('import')}<span class="nav-label">Importar</span>
          </div>
          <div class="nav-item" data-view="history">
            ${icon('history')}<span class="nav-label">Historico</span>
          </div>

          <div class="nav-section-title">Configuracion</div>
          <div class="nav-item" data-view="config" data-expandable="true" id="navConfig">
            ${icon('settings')}<span class="nav-label">Configuracion</span>
            <span class="nav-expand">${iconRaw('chevronDown')}</span>
          </div>
          <div class="nav-sub-items" id="configSubItems">
            <div class="nav-sub-item" data-view="categories" data-subview="rubros">Rubros</div>
            <div class="nav-sub-item" data-view="currencies" data-subview="monedas">Monedas</div>
            <div class="nav-sub-item" data-view="exchange-rates" data-subview="tipos-cambio">Tipos de Cambio</div>
            <div class="nav-sub-item" data-view="integrations" data-subview="integraciones">Integraciones</div>
            <div class="nav-sub-item" data-view="custom-fields" data-subview="campos">Campos Personalizados</div>
            <div class="nav-sub-item" data-view="audit" data-subview="auditoria">Auditoria</div>
          </div>
        </nav>

        <div class="sidebar-user">
          <div class="user-avatar">${initials}</div>
          <div class="user-info">
            <div class="user-name">${username}</div>
            <div class="user-role">${role}</div>
          </div>
          <button class="user-logout" id="logoutBtn" title="Cerrar sesion">
            ${iconRaw('logout')}
          </button>
        </div>
      </aside>

      <!-- MAIN CONTENT -->
      <main class="main-content">
        <header class="topbar" id="topbar">
          <div class="topbar-left">
            <span class="topbar-title">CashFlowApp</span>
          </div>
          <div class="topbar-center">
            <div class="month-nav">
              <button class="month-btn" id="prevMonth" title="Mes anterior">
                ${iconRaw('chevronLeft')}
              </button>
              <span class="month-label" id="monthLabel"></span>
              <button class="month-btn" id="nextMonth" title="Mes siguiente">
                ${iconRaw('chevronRight')}
              </button>
            </div>
            <div class="currency-toggle">
              <button class="cur-btn active" data-cur="ARS">$ Pesos</button>
              <button class="cur-btn" data-cur="USD">USD</button>
            </div>
          </div>
          <div class="topbar-right" id="topbarActions">
            <button class="btn btn-primary btn-sm" id="btnNewIngreso">
              ${iconRaw('plus')}<span>Ingreso</span>
            </button>
            <button class="btn btn-danger btn-sm" id="btnNewEgreso">
              ${iconRaw('plus')}<span>Egreso</span>
            </button>
            <button class="btn btn-info btn-sm" id="btnNewTransfer">
              ${iconRaw('arrowLeftRight')}<span>Transferencia</span>
            </button>
          </div>
        </header>

        <div class="page-content" id="pageContent">
          <div class="loading-overlay"><div class="spinner"></div></div>
        </div>
      </main>
    </div>

    <!-- TRANSACTION MODAL -->
    <div class="modal-overlay" id="txModal">
      <div class="modal">
        <div class="modal-header">
          <h2 id="txModalTitle">Nuevo Movimiento</h2>
          <button class="modal-close" data-close-modal="txModal">${iconRaw('x')}</button>
        </div>
        <div class="modal-body">
          <div class="type-tabs" id="typeTabs">
            <div class="type-tab active-ingreso" data-type="ingreso">Ingreso</div>
            <div class="type-tab" data-type="egreso">Egreso</div>
            <div class="type-tab" data-type="transferencia">Transferencia</div>
          </div>
          <form id="txForm">
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Fecha <span class="required">*</span></label>
                <input type="datetime-local" class="form-input" id="txDate" required />
              </div>
              <div class="form-group">
                <label class="form-label">Cuenta <span class="required">*</span></label>
                <select class="form-select" id="txAccount" required></select>
              </div>
            </div>
            <div class="form-group" id="txDestAccountGroup" style="display:none;">
              <label class="form-label">Cuenta Destino <span class="required">*</span></label>
              <select class="form-select" id="txDestAccount"></select>
            </div>
            <div class="form-group">
              <label class="form-label">Importe <span class="required">*</span></label>
              <div class="input-with-prefix">
                <span class="input-prefix">$</span>
                <input type="number" class="form-input" id="txAmount" step="0.01" min="0" required />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Descripcion <span class="required">*</span></label>
              <input type="text" class="form-input" id="txDescription" required />
            </div>
            <div class="form-group">
              <label class="form-label">Detalle</label>
              <textarea class="form-textarea" id="txDetail" rows="2"></textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Rubro</label>
                <select class="form-select" id="txCategory">
                  <option value="">Sin rubro</option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">SubRubro</label>
                <select class="form-select" id="txSubCategory">
                  <option value="">Sin subrubro</option>
                </select>
              </div>
            </div>
          </form>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" data-close-modal="txModal">Cancelar</button>
          <button class="btn btn-primary" id="txSaveBtn">Guardar</button>
        </div>
      </div>
    </div>

    <!-- ACCOUNT MODAL -->
    <div class="modal-overlay" id="accountModal">
      <div class="modal">
        <div class="modal-header">
          <h2 id="accountModalTitle">Nueva Cuenta</h2>
          <button class="modal-close" data-close-modal="accountModal">${iconRaw('x')}</button>
        </div>
        <div class="modal-body">
          <form id="accountForm">
            <div class="form-group">
              <label class="form-label">Nombre <span class="required">*</span></label>
              <input type="text" class="form-input" id="accName" required />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Tipo <span class="required">*</span></label>
                <select class="form-select" id="accType" required>
                  <option value="Banco">Banco</option>
                  <option value="Efectivo">Efectivo</option>
                  <option value="Tarjeta">Tarjeta</option>
                  <option value="Inversion">Inversion</option>
                  <option value="Otro">Otro</option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Moneda <span class="required">*</span></label>
                <select class="form-select" id="accCurrency" required>
                  <option value="ARS">ARS (Pesos)</option>
                  <option value="USD">USD (Dolares)</option>
                </select>
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Saldo Inicial</label>
              <div class="input-with-prefix">
                <span class="input-prefix">$</span>
                <input type="number" class="form-input" id="accBalance" step="0.01" value="0" />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Color</label>
              <div class="color-picker-wrap">
                <input type="color" id="accColor" value="#10b981" />
                <span id="accColorLabel">#10b981</span>
              </div>
            </div>
          </form>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" data-close-modal="accountModal">Cancelar</button>
          <button class="btn btn-primary" id="accSaveBtn">Guardar</button>
        </div>
      </div>
    </div>

    <!-- CATEGORY MODAL -->
    <div class="modal-overlay" id="categoryModal">
      <div class="modal">
        <div class="modal-header">
          <h2 id="categoryModalTitle">Nuevo Rubro</h2>
          <button class="modal-close" data-close-modal="categoryModal">${iconRaw('x')}</button>
        </div>
        <div class="modal-body">
          <form id="categoryForm">
            <div class="form-group">
              <label class="form-label">Nombre <span class="required">*</span></label>
              <input type="text" class="form-input" id="catName" required />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Rubro Padre</label>
                <select class="form-select" id="catParent">
                  <option value="">Ninguno (es principal)</option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Color</label>
                <div class="color-picker-wrap">
                  <input type="color" id="catColor" value="#10b981" />
                  <span id="catColorLabel">#10b981</span>
                </div>
              </div>
            </div>
          </form>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" data-close-modal="categoryModal">Cancelar</button>
          <button class="btn btn-primary" id="catSaveBtn">Guardar</button>
        </div>
      </div>
    </div>
    `;
  }

  /* ==========================================================
     BIND EVENTS
     ========================================================== */
  function bindEvents() {
    /* Sidebar toggle */
    document.getElementById('sidebarToggle').addEventListener('click', () => {
      document.getElementById('sidebar').classList.toggle('collapsed');
    });

    /* Navigation */
    document.getElementById('sidebarNav').addEventListener('click', (e) => {
      const navItem = e.target.closest('.nav-item');
      const subItem = e.target.closest('.nav-sub-item');

      if (navItem && navItem.dataset.expandable) {
        navItem.classList.toggle('expanded');
        document.getElementById('configSubItems').classList.toggle('expanded');
        return;
      }

      if (navItem && navItem.dataset.view) {
        navigateTo(navItem.dataset.view);
      }

      if (subItem && subItem.dataset.view) {
        navigateTo(subItem.dataset.view, subItem.dataset.subview);
      }
    });

    /* Month navigation */
    document.getElementById('prevMonth').addEventListener('click', () => {
      currentMonth--;
      if (currentMonth < 1) { currentMonth = 12; currentYear--; }
      updateMonthLabel();
      loadCurrentView();
    });

    document.getElementById('nextMonth').addEventListener('click', () => {
      currentMonth++;
      if (currentMonth > 12) { currentMonth = 1; currentYear++; }
      updateMonthLabel();
      loadCurrentView();
    });

    /* Currency toggle */
    document.querySelectorAll('.cur-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('.cur-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        currentCurrency = btn.dataset.cur;
        loadCurrentView();
      });
    });

    /* Quick action buttons */
    document.getElementById('btnNewIngreso').addEventListener('click', () => openTransactionModal('ingreso'));
    document.getElementById('btnNewEgreso').addEventListener('click', () => openTransactionModal('egreso'));
    document.getElementById('btnNewTransfer').addEventListener('click', () => openTransactionModal('transferencia'));

    /* Modal close buttons */
    document.querySelectorAll('[data-close-modal]').forEach(btn => {
      btn.addEventListener('click', () => {
        const modalId = btn.dataset.closeModal;
        closeModal(modalId);
      });
    });

    /* Modal overlay click to close */
    ['txModal', 'accountModal', 'categoryModal'].forEach(id => {
      document.getElementById(id).addEventListener('click', (e) => {
        if (e.target === e.currentTarget) closeModal(id);
      });
    });

    /* Transaction type tabs */
    document.getElementById('typeTabs').addEventListener('click', (e) => {
      const tab = e.target.closest('.type-tab');
      if (!tab) return;
      setTransactionType(tab.dataset.type);
    });

    /* Transaction category -> subcategory filter */
    document.getElementById('txCategory').addEventListener('change', () => {
      populateSubCategories();
    });

    /* Transaction save */
    document.getElementById('txSaveBtn').addEventListener('click', saveTransaction);

    /* Account save */
    document.getElementById('accSaveBtn').addEventListener('click', saveAccount);

    /* Account color preview */
    document.getElementById('accColor').addEventListener('input', (e) => {
      document.getElementById('accColorLabel').textContent = e.target.value;
    });

    /* Category save */
    document.getElementById('catSaveBtn').addEventListener('click', saveCategory);

    /* Category color preview */
    document.getElementById('catColor').addEventListener('input', (e) => {
      document.getElementById('catColorLabel').textContent = e.target.value;
    });

    /* Logout */
    document.getElementById('logoutBtn').addEventListener('click', () => Auth.logout());
  }

  /* ==========================================================
     NAVIGATION
     ========================================================== */
  function navigateTo(view, subview) {
    currentView = view;
    currentSubView = subview || null;

    document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
    document.querySelectorAll('.nav-sub-item').forEach(i => i.classList.remove('active'));

    const configViews = ['categories', 'currencies', 'exchange-rates', 'integrations', 'custom-fields', 'audit'];

    if (configViews.includes(view)) {
      document.getElementById('navConfig').classList.add('active');
      document.getElementById('navConfig').classList.add('expanded');
      document.getElementById('configSubItems').classList.add('expanded');
      const sub = document.querySelector('.nav-sub-item[data-view="' + view + '"]');
      if (sub) sub.classList.add('active');
    } else {
      const navItem = document.querySelector('.nav-item[data-view="' + view + '"]');
      if (navItem) navItem.classList.add('active');
    }

    loadCurrentView();
  }

  function loadCurrentView() {
    const page = document.getElementById('pageContent');
    page.innerHTML = '<div class="loading-overlay"><div class="spinner"></div></div>';

    switch (currentView) {
      case 'dashboard': loadDashboard(); break;
      case 'transactions': loadTransactions(); break;
      case 'accounts': loadAccounts(); break;
      case 'categories': loadCategories(); break;
      case 'exchange-rates': loadExchangeRates(); break;
      case 'import': loadImport(); break;
      case 'history': loadHistory(); break;
      case 'currencies': loadCurrencies(); break;
      case 'integrations': loadPlaceholder('Integraciones'); break;
      case 'custom-fields': loadPlaceholder('Campos Personalizados'); break;
      case 'audit': loadPlaceholder('Auditoria'); break;
      default: loadDashboard();
    }
  }

  function updateMonthLabel() {
    const label = document.getElementById('monthLabel');
    if (label) {
      label.textContent = MONTH_NAMES[currentMonth - 1] + ' ' + currentYear;
    }
  }

  /* ==========================================================
     MODAL HELPERS
     ========================================================== */
  function openModal(id) {
    document.getElementById(id).classList.add('active');
  }

  function closeModal(id) {
    document.getElementById(id).classList.remove('active');
  }

  function setTransactionType(type) {
    const tabs = document.querySelectorAll('#typeTabs .type-tab');
    tabs.forEach(t => {
      t.className = 'type-tab';
      if (t.dataset.type === type) {
        t.classList.add('active-' + type);
      }
    });

    const destGroup = document.getElementById('txDestAccountGroup');
    if (type === 'transferencia' || type === 'Transferencia') {
      destGroup.style.display = 'block';
    } else {
      destGroup.style.display = 'none';
    }
  }

  /* ==========================================================
     OPEN TRANSACTION MODAL
     ========================================================== */
  function openTransactionModal(type, tx) {
    editingTransactionId = tx ? tx.id : null;
    document.getElementById('txModalTitle').textContent = tx ? 'Editar Movimiento' : 'Nuevo Movimiento';

    setTransactionType(type || 'ingreso');

    /* Populate account dropdowns */
    const accOptions = accountsList.map(a =>
      '<option value="' + a.id + '">' + a.name + ' (' + a.currency + ')</option>'
    ).join('');
    document.getElementById('txAccount').innerHTML = accOptions;
    document.getElementById('txDestAccount').innerHTML = accOptions;

    /* Populate category dropdown */
    const catOptions = '<option value="">Sin rubro</option>' +
      categoriesList
        .filter(c => !c.parentId)
        .map(c => '<option value="' + c.id + '">' + c.name + '</option>')
        .join('');
    document.getElementById('txCategory').innerHTML = catOptions;
    document.getElementById('txSubCategory').innerHTML = '<option value="">Sin subrubro</option>';

    /* Fill values */
    if (tx) {
      document.getElementById('txDate').value = formatDateTimeLocal(tx.date);
      document.getElementById('txAccount').value = tx.accountId || '';
      document.getElementById('txDestAccount').value = tx.destinationAccountId || '';
      document.getElementById('txAmount').value = Math.abs(tx.amount || 0);
      document.getElementById('txDescription').value = tx.description || '';
      document.getElementById('txDetail').value = tx.detail || '';
      document.getElementById('txCategory').value = tx.categoryId || '';
      populateSubCategories();
      document.getElementById('txSubCategory').value = tx.subCategoryId || '';
    } else {
      document.getElementById('txDate').value = formatDateTimeLocal();
      document.getElementById('txAmount').value = '';
      document.getElementById('txDescription').value = '';
      document.getElementById('txDetail').value = '';
      document.getElementById('txCategory').value = '';
      document.getElementById('txSubCategory').innerHTML = '<option value="">Sin subrubro</option>';
    }

    openModal('txModal');
  }

  function populateSubCategories() {
    const parentId = document.getElementById('txCategory').value;
    const subSelect = document.getElementById('txSubCategory');
    subSelect.innerHTML = '<option value="">Sin subrubro</option>';

    if (!parentId) return;

    const parent = categoriesList.find(c => String(c.id) === String(parentId));
    if (parent && parent.subCategories && parent.subCategories.length > 0) {
      parent.subCategories.forEach(sub => {
        subSelect.innerHTML += '<option value="' + sub.id + '">' + sub.name + '</option>';
      });
    }
  }

  /* ==========================================================
     SAVE TRANSACTION
     ========================================================== */
  async function saveTransaction() {
    const activeTab = document.querySelector('#typeTabs .type-tab[class*="active-"]');
    let type = 'ingreso';
    if (activeTab) {
      type = activeTab.dataset.type;
    }

    const capitalType = type.charAt(0).toUpperCase() + type.slice(1);
    const data = {
      accountId: document.getElementById('txAccount').value,
      type: capitalType,
      amount: parseFloat(document.getElementById('txAmount').value) || 0,
      description: document.getElementById('txDescription').value.trim(),
      detail: document.getElementById('txDetail').value.trim(),
      date: document.getElementById('txDate').value,
      categoryId: document.getElementById('txCategory').value || null,
      subCategoryId: document.getElementById('txSubCategory').value || null
    };

    if (capitalType === 'Transferencia') {
      data.destinationAccountId = document.getElementById('txDestAccount').value;
    }

    if (!data.accountId || !data.amount || !data.description) {
      showToast('Completa los campos obligatorios', 'error');
      return;
    }

    try {
      if (editingTransactionId) {
        await Api.updateTransaction(editingTransactionId, data);
        showToast('Movimiento actualizado');
      } else {
        await Api.createTransaction(data);
        showToast('Movimiento creado');
      }
      closeModal('txModal');
      loadCurrentView();
    } catch (err) {
      showToast(err.message, 'error');
    }
  }

  /* ==========================================================
     OPEN ACCOUNT MODAL
     ========================================================== */
  function openAccountModal(account) {
    editingAccountId = account ? account.id : null;
    document.getElementById('accountModalTitle').textContent = account ? 'Editar Cuenta' : 'Nueva Cuenta';

    if (account) {
      document.getElementById('accName').value = account.name || '';
      document.getElementById('accType').value = account.type || 'Banco';
      document.getElementById('accCurrency').value = account.currency || 'ARS';
      document.getElementById('accBalance').value = account.balance || 0;
      document.getElementById('accColor').value = account.color || '#10b981';
      document.getElementById('accColorLabel').textContent = account.color || '#10b981';
    } else {
      document.getElementById('accName').value = '';
      document.getElementById('accType').value = 'Banco';
      document.getElementById('accCurrency').value = 'ARS';
      document.getElementById('accBalance').value = 0;
      document.getElementById('accColor').value = '#10b981';
      document.getElementById('accColorLabel').textContent = '#10b981';
    }

    openModal('accountModal');
  }

  /* ==========================================================
     SAVE ACCOUNT
     ========================================================== */
  async function saveAccount() {
    const data = {
      name: document.getElementById('accName').value.trim(),
      type: document.getElementById('accType').value,
      currency: document.getElementById('accCurrency').value,
      balance: parseFloat(document.getElementById('accBalance').value) || 0,
      color: document.getElementById('accColor').value
    };

    if (!data.name) {
      showToast('Ingresa un nombre para la cuenta', 'error');
      return;
    }

    try {
      if (editingAccountId) {
        await Api.updateAccount(editingAccountId, data);
        showToast('Cuenta actualizada');
      } else {
        await Api.createAccount(data);
        showToast('Cuenta creada');
      }
      closeModal('accountModal');
      await loadAccountsData();
      loadCurrentView();
    } catch (err) {
      showToast(err.message, 'error');
    }
  }

  /* ==========================================================
     OPEN CATEGORY MODAL
     ========================================================== */
  function openCategoryModal(category) {
    editingCategoryId = category ? category.id : null;
    document.getElementById('categoryModalTitle').textContent = category ? 'Editar Rubro' : 'Nuevo Rubro';

    /* Populate parent dropdown (only top-level) */
    const parentOptions = '<option value="">Ninguno (es principal)</option>' +
      categoriesList
        .filter(c => !c.parentId && (!category || String(c.id) !== String(category.id)))
        .map(c => '<option value="' + c.id + '">' + c.name + '</option>')
        .join('');
    document.getElementById('catParent').innerHTML = parentOptions;

    if (category) {
      document.getElementById('catName').value = category.name || '';
      document.getElementById('catParent').value = category.parentId || '';
      document.getElementById('catColor').value = category.color || '#10b981';
      document.getElementById('catColorLabel').textContent = category.color || '#10b981';
    } else {
      document.getElementById('catName').value = '';
      document.getElementById('catParent').value = '';
      document.getElementById('catColor').value = '#10b981';
      document.getElementById('catColorLabel').textContent = '#10b981';
    }

    openModal('categoryModal');
  }

  /* ==========================================================
     SAVE CATEGORY
     ========================================================== */
  async function saveCategory() {
    const data = {
      name: document.getElementById('catName').value.trim(),
      parentId: document.getElementById('catParent').value || null,
      color: document.getElementById('catColor').value
    };

    if (!data.name) {
      showToast('Ingresa un nombre para el rubro', 'error');
      return;
    }

    try {
      if (editingCategoryId) {
        await Api.updateCategory(editingCategoryId, data);
        showToast('Rubro actualizado');
      } else {
        await Api.createCategory(data);
        showToast('Rubro creado');
      }
      closeModal('categoryModal');
      await loadCategoriesData();
      loadCurrentView();
    } catch (err) {
      showToast(err.message, 'error');
    }
  }

  /* ==========================================================
     LOAD REFERENCE DATA
     ========================================================== */
  async function loadAccountsData() {
    try {
      accountsList = await Api.getAccounts() || [];
    } catch { accountsList = []; }
  }

  async function loadCategoriesData() {
    try {
      categoriesList = await Api.getCategories() || [];
    } catch { categoriesList = []; }
  }

  async function loadExchangeRateData() {
    try {
      const rates = await Api.getExchangeRates() || [];
      const usdRate = rates.find(r => r.fromCurrency === 'USD' && r.toCurrency === 'ARS');
      if (usdRate) exchangeRate = usdRate.rate || 1;
    } catch { exchangeRate = 1; }
  }

  /* ==========================================================
     VIEW: DASHBOARD
     ========================================================== */
  async function loadDashboard() {
    const page = document.getElementById('pageContent');

    try {
      const [stats, accounts, expenses, trend] = await Promise.all([
        Api.getDashboardStats(currentMonth, currentYear).catch(() => null),
        Api.getDashboardAccounts().catch(() => null),
        Api.getExpensesByCategory(currentMonth, currentYear).catch(() => null),
        Api.getMonthlyTrend(12).catch(() => null)
      ]);

      const s = stats || { totalIncome: 0, totalExpenses: 0, balance: 0, totalTransactions: 0 };
      const accs = accounts || [];
      const exps = expenses || [];
      const tr = trend || [];

      /* Compute totals for summary */
      let totalARS = 0;
      let totalUSD = 0;
      let accCount = accs.length || accountsList.length;
      accs.forEach(a => {
        if (a.currency === 'USD') totalUSD += (a.balance || 0);
        else totalARS += (a.balance || 0);
      });

      /* Group accounts by type */
      const accountsByType = {};
      accs.forEach(a => {
        const t = a.type || 'Otro';
        if (!accountsByType[t]) accountsByType[t] = [];
        accountsByType[t].push(a);
      });

      const typeIconMap = {
        'Banco': 'bank',
        'Efectivo': 'cash',
        'Tarjeta': 'creditCard',
        'Inversion': 'trendUp'
      };

      let accountTypeCardsHTML = '';
      Object.keys(accountsByType).forEach(type => {
        const accsOfType = accountsByType[type];
        let typeTotal = 0;
        accsOfType.forEach(a => {
          typeTotal += a.currency === 'USD' ? (a.balanceUsd || a.balance || 0) * exchangeRate : (a.balance || 0);
        });
        const iconName = typeIconMap[type] || 'wallet';

        accountTypeCardsHTML += `
          <div class="account-type-card" style="border-left-color: ${accsOfType[0].color || 'var(--primary)'}">
            <div class="atc-header">
              <div class="atc-type">
                <div class="atc-type-icon" style="background: ${(accsOfType[0].color || '#10b981') + '1a'}">
                  <span style="color: ${accsOfType[0].color || '#10b981'}">${iconRaw(iconName)}</span>
                </div>
                ${type}
              </div>
              <span style="font-size:0.75rem;color:var(--text-muted)">${accsOfType.length} cuenta${accsOfType.length > 1 ? 's' : ''}</span>
            </div>
            <div class="atc-amount">${formatCurrency(typeTotal)}</div>
            <div class="atc-changes">
              <div class="atc-change positive">
                ${iconRaw('trendUp')}
                <span class="change-period">1D</span>
              </div>
              <div class="atc-change positive">
                ${iconRaw('trendUp')}
                <span class="change-period">7D</span>
              </div>
              <div class="atc-change positive">
                ${iconRaw('trendUp')}
                <span class="change-period">1M</span>
              </div>
            </div>
          </div>
        `;
      });

      page.innerHTML = `
        <!-- Stats Row -->
        <div class="stats-row">
          <div class="stat-card stat-income">
            <div class="stat-header">
              <span class="stat-label">Ingresos</span>
              <div class="stat-icon">${iconRaw('arrowDown')}</div>
            </div>
            <div class="stat-value">${formatCurrency(s.totalIncome)}</div>
            <div class="stat-sub">${MONTH_NAMES[currentMonth - 1]} ${currentYear}</div>
          </div>
          <div class="stat-card stat-expense">
            <div class="stat-header">
              <span class="stat-label">Egresos</span>
              <div class="stat-icon">${iconRaw('arrowUp')}</div>
            </div>
            <div class="stat-value">${formatCurrency(Math.abs(s.totalExpenses))}</div>
            <div class="stat-sub">${MONTH_NAMES[currentMonth - 1]} ${currentYear}</div>
          </div>
          <div class="stat-card stat-balance">
            <div class="stat-header">
              <span class="stat-label">Balance</span>
              <div class="stat-icon">${iconRaw('trendUp')}</div>
            </div>
            <div class="stat-value">${formatCurrency(s.balance)}</div>
            <div class="stat-sub">${MONTH_NAMES[currentMonth - 1]} ${currentYear}</div>
          </div>
          <div class="stat-card stat-movements">
            <div class="stat-header">
              <span class="stat-label">Movimientos</span>
              <div class="stat-icon">${iconRaw('transactions')}</div>
            </div>
            <div class="stat-value">${s.totalTransactions || 0}</div>
            <div class="stat-sub">${MONTH_NAMES[currentMonth - 1]} ${currentYear}</div>
          </div>
        </div>

        <!-- Summary Card -->
        <div class="summary-card">
          <div class="summary-icon">${iconRaw('wallet')}</div>
          <div class="summary-info">
            <div class="summary-label">Saldo Total</div>
            <div class="summary-value">${formatCurrency(totalARS)} / USD ${formatNumber(totalUSD)}</div>
            <div class="summary-sub">${accCount} cuenta${accCount !== 1 ? 's' : ''}</div>
          </div>
        </div>

        <!-- Account Type Cards -->
        ${accountTypeCardsHTML ? '<div class="accounts-section"><div class="section-title">Saldos por Tipo de Cuenta</div><div class="account-type-grid">' + accountTypeCardsHTML + '</div></div>' : ''}

        <!-- Charts Row -->
        <div class="charts-row">
          <div class="card">
            <div class="card-header">
              <h3>${iconRaw('pieChart')} Egresos por Rubro</h3>
            </div>
            <div class="card-body">
              <div class="chart-container">
                <canvas id="donutChart"></canvas>
              </div>
            </div>
          </div>
          <div class="card">
            <div class="card-header">
              <h3>${iconRaw('barChart')} Tendencia (12 meses)</h3>
            </div>
            <div class="card-body">
              <div class="chart-container">
                <canvas id="barChart"></canvas>
              </div>
            </div>
          </div>
        </div>
      `;

      /* Build charts */
      buildDonutChart(exps);
      buildBarChart(tr);

    } catch (err) {
      page.innerHTML = '<div class="empty-state">' + iconRaw('warning') + '<h4>Error al cargar el dashboard</h4><p>' + (err.message || '') + '</p></div>';
    }
  }

  /* ==========================================================
     CHARTS
     ========================================================== */
  function buildDonutChart(data) {
    const canvas = document.getElementById('donutChart');
    if (!canvas) return;

    if (chartDonut) { chartDonut.destroy(); chartDonut = null; }

    if (!data || data.length === 0) {
      canvas.parentElement.innerHTML = '<div class="empty-state" style="padding:20px"><p>Sin datos para este periodo</p></div>';
      return;
    }

    const labels = data.map(d => d.categoryName || 'Sin rubro');
    const values = data.map(d => d.total || 0);
    const colors = data.map(d => d.color || '#94a3b8');

    chartDonut = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: labels,
        datasets: [{
          data: values,
          backgroundColor: colors,
          borderWidth: 2,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              padding: 16,
              usePointStyle: true,
              pointStyle: 'circle',
              font: { size: 11, family: 'Inter' }
            }
          },
          tooltip: {
            callbacks: {
              label: function(ctx) {
                const val = ctx.parsed || 0;
                const pct = data[ctx.dataIndex] ? data[ctx.dataIndex].percentage : 0;
                return ctx.label + ': ' + formatCurrency(val) + ' (' + (pct || 0).toFixed(1) + '%)';
              }
            }
          }
        },
        cutout: '60%'
      }
    });
  }

  function buildBarChart(data) {
    const canvas = document.getElementById('barChart');
    if (!canvas) return;

    if (chartBar) { chartBar.destroy(); chartBar = null; }

    if (!data || data.length === 0) {
      canvas.parentElement.innerHTML = '<div class="empty-state" style="padding:20px"><p>Sin datos para este periodo</p></div>';
      return;
    }

    const labels = data.map(d => MONTH_NAMES[(d.month || 1) - 1].substring(0, 3) + ' ' + (d.year || ''));
    const incomes = data.map(d => d.income || 0);
    const expenses = data.map(d => Math.abs(d.expense || 0));

    chartBar = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Ingresos',
            data: incomes,
            backgroundColor: 'rgba(16, 185, 129, 0.7)',
            borderColor: '#10b981',
            borderWidth: 1,
            borderRadius: 4,
            barPercentage: 0.6
          },
          {
            label: 'Egresos',
            data: expenses,
            backgroundColor: 'rgba(239, 68, 68, 0.7)',
            borderColor: '#ef4444',
            borderWidth: 1,
            borderRadius: 4,
            barPercentage: 0.6
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            position: 'top',
            labels: {
              padding: 16,
              usePointStyle: true,
              pointStyle: 'circle',
              font: { size: 11, family: 'Inter' }
            }
          },
          tooltip: {
            callbacks: {
              label: function(ctx) {
                return ctx.dataset.label + ': ' + formatCurrency(ctx.parsed.y);
              }
            }
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { font: { size: 10, family: 'Inter' } }
          },
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(0,0,0,0.05)' },
            ticks: {
              font: { size: 10, family: 'Inter' },
              callback: function(v) { return formatCurrency(v); }
            }
          }
        }
      }
    });
  }

  /* ==========================================================
     VIEW: TRANSACTIONS
     ========================================================== */
  async function loadTransactions() {
    const page = document.getElementById('pageContent');

    try {
      const txFilters = {
        type: txFilterType,
        category: txFilterCategory,
        search: txFilterSearch,
        account: txFilterAccount,
        page: txPage,
        pageSize: txPageSize
      };
      if (txDateRangeMode && txFilterDateFrom && txFilterDateTo) {
        txFilters.dateFrom = txFilterDateFrom;
        txFilters.dateTo = txFilterDateTo;
      } else {
        txFilters.month = currentMonth;
        txFilters.year = currentYear;
      }
      const result = await Api.getTransactions(txFilters);

      const txData = result ? (result.data || []) : [];
      txTotal = result ? (result.total || 0) : 0;
      const totalPages = Math.ceil(txTotal / txPageSize) || 1;

      let rowsHTML = '';
      txData.forEach(tx => {
        const txType = (tx.type || '').toLowerCase();
        const typeClass = txType === 'ingreso' ? 'type-ingreso' : txType === 'egreso' ? 'type-egreso' : 'type-transferencia';
        const typeIcon = txType === 'ingreso' ? iconRaw('arrowDown') : txType === 'egreso' ? iconRaw('arrowUp') : iconRaw('arrowLeftRight');
        const amount = txType === 'egreso' ? -Math.abs(tx.amount || 0) : (tx.amount || 0);
        const catColor = tx.categoryColor || '#94a3b8';
        const catName = tx.categoryName || tx.category || '';
        const catBadgeBg = catColor + '1a';

        rowsHTML += `
          <tr>
            <td><input type="checkbox" class="row-checkbox" /></td>
            <td>
              <div class="tx-date">
                ${formatDate(tx.date)}
                <div class="tx-time">${formatTime(tx.date)}</div>
              </div>
            </td>
            <td><span class="type-icon ${typeClass}">${typeIcon}</span></td>
            <td>
              ${catName ? '<span class="category-badge" style="background:' + catBadgeBg + ';color:' + catColor + '"><span class="cat-dot" style="background:' + catColor + '"></span>' + catName + '</span>' : '<span class="text-muted">-</span>'}
            </td>
            <td><div class="tx-desc">${tx.description || ''}</div></td>
            <td>
              <div class="tx-account">
                <div class="account-icon">${iconRaw('wallet')}</div>
                <span>${tx.accountName || ''}</span>
              </div>
            </td>
            <td>${tx.lotReference || ''}</td>
            <td class="text-right">${formatCurrencyColored(amount)}</td>
            <td>
              <div class="row-actions">
                <button class="btn-edit" title="Editar" data-edit-tx='${JSON.stringify(tx).replace(/'/g, "&#39;")}'>${iconRaw('edit')}</button>
                <button class="btn-delete" title="Eliminar" data-delete-tx="${tx.id}">${iconRaw('trash')}</button>
              </div>
            </td>
          </tr>
        `;
      });

      /* Category filter options */
      const catFilterOptions = '<option value="">Todas</option>' +
        categoriesList
          .filter(c => !c.parentId)
          .map(c => '<option value="' + c.id + '"' + (txFilterCategory === String(c.id) ? ' selected' : '') + '>' + c.name + '</option>')
          .join('');

      /* Account filter options */
      const accFilterOptions = '<option value="">Todas</option>' +
        accountsList.map(a => '<option value="' + a.id + '"' + (txFilterAccount === String(a.id) ? ' selected' : '') + '>' + a.name + '</option>').join('');

      /* Pagination HTML */
      let paginationHTML = '';
      paginationHTML += '<button class="page-btn" data-page="' + (txPage - 1) + '"' + (txPage <= 1 ? ' disabled' : '') + '>' + iconRaw('chevronLeft') + '</button>';
      for (let i = 1; i <= totalPages && i <= 7; i++) {
        paginationHTML += '<button class="page-btn' + (i === txPage ? ' active' : '') + '" data-page="' + i + '">' + i + '</button>';
      }
      if (totalPages > 7) {
        paginationHTML += '<span class="text-muted">...</span>';
        paginationHTML += '<button class="page-btn' + (totalPages === txPage ? ' active' : '') + '" data-page="' + totalPages + '">' + totalPages + '</button>';
      }
      paginationHTML += '<button class="page-btn" data-page="' + (txPage + 1) + '"' + (txPage >= totalPages ? ' disabled' : '') + '>' + iconRaw('chevronRight') + '</button>';

      page.innerHTML = `
        <div class="view-header">
          <div>
            <h2>Gestion de Movimientos</h2>
            <span class="record-count">${txData.length}/${txTotal} registros</span>
          </div>
          <div class="view-header-actions">
            <button class="btn btn-secondary btn-sm" id="txClear">${iconRaw('x')} Limpiar</button>
            <button class="btn btn-primary btn-sm" id="txNewIngreso">${iconRaw('plus')} Ingreso</button>
            <button class="btn btn-danger btn-sm" id="txNewEgreso">${iconRaw('plus')} Egreso</button>
            <button class="btn btn-info btn-sm" id="txNewTransfer">${iconRaw('arrowLeftRight')} Transferencia</button>
            <button class="btn btn-secondary btn-sm">${iconRaw('download')} Exportar</button>
            <button class="btn btn-secondary btn-sm">${iconRaw('upload')} Importar</button>
            <button class="btn btn-secondary btn-sm">${iconRaw('refresh')} Sincronizar</button>
          </div>
        </div>

        <div class="card">
          <div class="table-toolbar">
            <div class="table-toolbar-left">
              <select class="filter-select" id="txTypeFilter">
                <option value="">Tipo: Todos</option>
                <option value="Ingreso"${txFilterType === 'Ingreso' ? ' selected' : ''}>Ingreso</option>
                <option value="Egreso"${txFilterType === 'Egreso' ? ' selected' : ''}>Egreso</option>
                <option value="Transferencia"${txFilterType === 'Transferencia' ? ' selected' : ''}>Transferencia</option>
              </select>
              <select class="filter-select" id="txCatFilter">${catFilterOptions}</select>
              <select class="filter-select" id="txAccFilter">${accFilterOptions}</select>
              <div class="date-range-group">
                <label class="date-range-toggle">
                  <input type="checkbox" id="txDateRangeToggle" ${txDateRangeMode ? 'checked' : ''} />
                  <span>Rango</span>
                </label>
                <input type="date" class="filter-date" id="txDateFrom" value="${txFilterDateFrom}" ${txDateRangeMode ? '' : 'disabled'} />
                <span class="date-sep">a</span>
                <input type="date" class="filter-date" id="txDateTo" value="${txFilterDateTo}" ${txDateRangeMode ? '' : 'disabled'} />
              </div>
            </div>
            <div class="table-toolbar-right">
              <div class="table-search">
                ${iconRaw('search')}
                <input type="text" placeholder="Buscar movimientos..." id="txSearchInput" value="${txFilterSearch}" />
              </div>
            </div>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th style="width:40px"><input type="checkbox" class="row-checkbox" id="txSelectAll" /></th>
                <th>Fecha</th>
                <th>Tipo</th>
                <th>Categoria</th>
                <th>Descripcion</th>
                <th>Cuenta</th>
                <th>Lote</th>
                <th class="text-right">Importe</th>
                <th style="width:80px"></th>
              </tr>
            </thead>
            <tbody id="txTableBody">
              ${rowsHTML || '<tr><td colspan="9"><div class="empty-state"><h4>Sin movimientos</h4><p>No hay movimientos para este periodo</p></div></td></tr>'}
            </tbody>
          </table>

          <div class="table-footer">
            <span>Pagina ${txPage} de ${totalPages} (${txTotal} total)</span>
            <div class="pagination" id="txPagination">
              ${paginationHTML}
            </div>
          </div>
        </div>
      `;

      /* Bind transaction view events */
      bindTransactionEvents();

    } catch (err) {
      page.innerHTML = '<div class="empty-state">' + iconRaw('warning') + '<h4>Error al cargar movimientos</h4><p>' + (err.message || '') + '</p></div>';
    }
  }

  function bindTransactionEvents() {
    /* Filter events */
    const typeFilter = document.getElementById('txTypeFilter');
    if (typeFilter) {
      typeFilter.addEventListener('change', () => {
        txFilterType = typeFilter.value;
        txPage = 1;
        loadTransactions();
      });
    }

    const catFilter = document.getElementById('txCatFilter');
    if (catFilter) {
      catFilter.addEventListener('change', () => {
        txFilterCategory = catFilter.value;
        txPage = 1;
        loadTransactions();
      });
    }

    const accFilter = document.getElementById('txAccFilter');
    if (accFilter) {
      accFilter.addEventListener('change', () => {
        txFilterAccount = accFilter.value;
        txPage = 1;
        loadTransactions();
      });
    }

    const searchInput = document.getElementById('txSearchInput');
    if (searchInput) {
      let searchTimeout;
      searchInput.addEventListener('input', () => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
          txFilterSearch = searchInput.value.trim();
          txPage = 1;
          loadTransactions();
        }, 400);
      });
    }

    /* Date range toggle and inputs */
    const dateRangeToggle = document.getElementById('txDateRangeToggle');
    const dateFromInput = document.getElementById('txDateFrom');
    const dateToInput = document.getElementById('txDateTo');
    if (dateRangeToggle) {
      dateRangeToggle.addEventListener('change', () => {
        txDateRangeMode = dateRangeToggle.checked;
        dateFromInput.disabled = !txDateRangeMode;
        dateToInput.disabled = !txDateRangeMode;
        if (!txDateRangeMode) {
          txFilterDateFrom = '';
          txFilterDateTo = '';
        }
        txPage = 1;
        loadTransactions();
      });
    }
    if (dateFromInput) {
      dateFromInput.addEventListener('change', () => {
        txFilterDateFrom = dateFromInput.value;
        if (txFilterDateFrom && txFilterDateTo) { txPage = 1; loadTransactions(); }
      });
    }
    if (dateToInput) {
      dateToInput.addEventListener('change', () => {
        txFilterDateTo = dateToInput.value;
        if (txFilterDateFrom && txFilterDateTo) { txPage = 1; loadTransactions(); }
      });
    }

    /* Clear filters */
    const clearBtn = document.getElementById('txClear');
    if (clearBtn) {
      clearBtn.addEventListener('click', () => {
        txFilterType = '';
        txFilterCategory = '';
        txFilterSearch = '';
        txFilterAccount = '';
        txFilterDateFrom = '';
        txFilterDateTo = '';
        txDateRangeMode = false;
        txPage = 1;
        loadTransactions();
      });
    }

    /* Pagination */
    const pagination = document.getElementById('txPagination');
    if (pagination) {
      pagination.addEventListener('click', (e) => {
        const btn = e.target.closest('.page-btn');
        if (!btn || btn.disabled) return;
        txPage = parseInt(btn.dataset.page);
        loadTransactions();
      });
    }

    /* New transaction buttons */
    const newIngreso = document.getElementById('txNewIngreso');
    if (newIngreso) newIngreso.addEventListener('click', () => openTransactionModal('ingreso'));

    const newEgreso = document.getElementById('txNewEgreso');
    if (newEgreso) newEgreso.addEventListener('click', () => openTransactionModal('egreso'));

    const newTransfer = document.getElementById('txNewTransfer');
    if (newTransfer) newTransfer.addEventListener('click', () => openTransactionModal('transferencia'));

    /* Edit/Delete transaction */
    const tbody = document.getElementById('txTableBody');
    if (tbody) {
      tbody.addEventListener('click', (e) => {
        const editBtn = e.target.closest('[data-edit-tx]');
        if (editBtn) {
          try {
            const tx = JSON.parse(editBtn.dataset.editTx);
            openTransactionModal(tx.type || 'ingreso', tx);
          } catch {}
          return;
        }

        const deleteBtn = e.target.closest('[data-delete-tx]');
        if (deleteBtn) {
          const txId = deleteBtn.dataset.deleteTx;
          showConfirm('Eliminar Movimiento', 'Esta seguro que desea eliminar este movimiento?', async () => {
            try {
              await Api.deleteTransaction(txId);
              showToast('Movimiento eliminado');
              loadTransactions();
            } catch (err) {
              showToast(err.message, 'error');
            }
          });
        }
      });
    }

    /* Select all checkbox */
    const selectAll = document.getElementById('txSelectAll');
    if (selectAll) {
      selectAll.addEventListener('change', () => {
        document.querySelectorAll('#txTableBody .row-checkbox').forEach(cb => {
          cb.checked = selectAll.checked;
        });
      });
    }
  }

  /* ==========================================================
     VIEW: ACCOUNTS
     ========================================================== */
  async function loadAccounts() {
    const page = document.getElementById('pageContent');

    try {
      await loadAccountsData();

      let cardsHTML = '';
      accountsList.forEach(acc => {
        const borderColor = acc.color || 'var(--primary)';
        cardsHTML += `
          <div class="account-card" style="border-left-color: ${borderColor}">
            <div class="ac-header">
              <div>
                <div class="ac-name">${acc.name || ''}</div>
                <div class="ac-type">${acc.type || ''}</div>
              </div>
              <span style="font-size:0.72rem;color:var(--text-muted)">${acc.currency || 'ARS'}</span>
            </div>
            <div class="ac-balance">${formatCurrency(acc.balance, acc.currency)}</div>
            <div class="ac-actions">
              <button class="btn btn-sm btn-outline" data-edit-acc='${JSON.stringify(acc).replace(/'/g, "&#39;")}'>
                ${iconRaw('edit')} Editar
              </button>
              <button class="btn btn-sm btn-outline" style="color:var(--danger);border-color:var(--danger-bg)" data-delete-acc="${acc.id}">
                ${iconRaw('trash')} Eliminar
              </button>
            </div>
          </div>
        `;
      });

      page.innerHTML = `
        <div class="view-header">
          <h2>Cuentas</h2>
          <div class="view-header-actions">
            <button class="btn btn-primary btn-sm" id="newAccountBtn">
              ${iconRaw('plus')} Nueva Cuenta
            </button>
          </div>
        </div>

        <div class="accounts-grid" id="accountsGrid">
          ${cardsHTML || '<div class="empty-state" style="grid-column:1/-1">' + iconRaw('wallet') + '<h4>Sin cuentas</h4><p>Crea tu primera cuenta para comenzar</p></div>'}
        </div>
      `;

      /* Bind events */
      const newAccBtn = document.getElementById('newAccountBtn');
      if (newAccBtn) newAccBtn.addEventListener('click', () => openAccountModal());

      const grid = document.getElementById('accountsGrid');
      if (grid) {
        grid.addEventListener('click', (e) => {
          const editBtn = e.target.closest('[data-edit-acc]');
          if (editBtn) {
            try {
              const acc = JSON.parse(editBtn.dataset.editAcc);
              openAccountModal(acc);
            } catch {}
            return;
          }

          const deleteBtn = e.target.closest('[data-delete-acc]');
          if (deleteBtn) {
            showConfirm('Eliminar Cuenta', 'Esta seguro que desea eliminar esta cuenta? Se perderan todos los movimientos asociados.', async () => {
              try {
                await Api.deleteAccount(deleteBtn.dataset.deleteAcc);
                showToast('Cuenta eliminada');
                loadAccounts();
              } catch (err) {
                showToast(err.message, 'error');
              }
            });
          }
        });
      }

    } catch (err) {
      page.innerHTML = '<div class="empty-state">' + iconRaw('warning') + '<h4>Error al cargar cuentas</h4><p>' + (err.message || '') + '</p></div>';
    }
  }

  /* ==========================================================
     VIEW: CATEGORIES (Rubros)
     ========================================================== */
  async function loadCategories() {
    const page = document.getElementById('pageContent');

    try {
      await loadCategoriesData();

      const topLevel = categoriesList.filter(c => !c.parentId);

      let treeHTML = '';
      topLevel.forEach(cat => {
        const subs = cat.subCategories || categoriesList.filter(c => String(c.parentId) === String(cat.id));
        let subsHTML = '';
        subs.forEach(sub => {
          subsHTML += `
            <div class="subcategory-item">
              <span class="category-color-dot" style="background:${sub.color || '#94a3b8'}"></span>
              <span class="category-name" style="font-weight:400;font-size:0.85rem">${sub.name}</span>
              <div class="category-actions" style="margin-left:auto">
                <button class="btn-edit" style="width:26px;height:26px;border-radius:4px;display:flex;align-items:center;justify-content:center;background:transparent;color:var(--primary);border:none;cursor:pointer" data-edit-cat='${JSON.stringify(sub).replace(/'/g, "&#39;")}'>${iconRaw('edit')}</button>
                <button class="btn-delete" style="width:26px;height:26px;border-radius:4px;display:flex;align-items:center;justify-content:center;background:transparent;color:var(--danger);border:none;cursor:pointer" data-delete-cat="${sub.id}">${iconRaw('trash')}</button>
              </div>
            </div>
          `;
        });

        treeHTML += `
          <div class="category-item">
            <div class="category-item-header">
              <span class="category-color-dot" style="background:${cat.color || '#10b981'}"></span>
              <span class="category-name">${cat.name}</span>
              <div class="category-actions">
                <button class="btn-edit" style="width:28px;height:28px;border-radius:4px;display:flex;align-items:center;justify-content:center;background:transparent;color:var(--primary);border:none;cursor:pointer" data-edit-cat='${JSON.stringify(cat).replace(/'/g, "&#39;")}'>${iconRaw('edit')}</button>
                <button class="btn-delete" style="width:28px;height:28px;border-radius:4px;display:flex;align-items:center;justify-content:center;background:transparent;color:var(--danger);border:none;cursor:pointer" data-delete-cat="${cat.id}">${iconRaw('trash')}</button>
              </div>
            </div>
            ${subsHTML ? '<div class="subcategory-list">' + subsHTML + '</div>' : ''}
          </div>
        `;
      });

      page.innerHTML = `
        <div class="view-header">
          <h2>Rubros</h2>
          <div class="view-header-actions">
            <button class="btn btn-primary btn-sm" id="newCatBtn">
              ${iconRaw('plus')} Nuevo Rubro
            </button>
          </div>
        </div>

        <div class="card">
          <div class="card-body">
            <div class="category-tree" id="categoryTree">
              ${treeHTML || '<div class="empty-state">' + iconRaw('tag') + '<h4>Sin rubros</h4><p>Crea tu primer rubro para categorizar movimientos</p></div>'}
            </div>
          </div>
        </div>
      `;

      /* Bind events */
      const newCatBtn = document.getElementById('newCatBtn');
      if (newCatBtn) newCatBtn.addEventListener('click', () => openCategoryModal());

      const tree = document.getElementById('categoryTree');
      if (tree) {
        tree.addEventListener('click', (e) => {
          const editBtn = e.target.closest('[data-edit-cat]');
          if (editBtn) {
            try {
              const cat = JSON.parse(editBtn.dataset.editCat);
              openCategoryModal(cat);
            } catch {}
            return;
          }

          const deleteBtn = e.target.closest('[data-delete-cat]');
          if (deleteBtn) {
            showConfirm('Eliminar Rubro', 'Esta seguro que desea eliminar este rubro?', async () => {
              try {
                await Api.deleteCategory(deleteBtn.dataset.deleteCat);
                showToast('Rubro eliminado');
                loadCategories();
              } catch (err) {
                showToast(err.message, 'error');
              }
            });
          }
        });
      }

    } catch (err) {
      page.innerHTML = '<div class="empty-state">' + iconRaw('warning') + '<h4>Error al cargar rubros</h4><p>' + (err.message || '') + '</p></div>';
    }
  }

  /* ==========================================================
     VIEW: EXCHANGE RATES (Tipos de Cambio)
     ========================================================== */
  async function loadExchangeRates() {
    const page = document.getElementById('pageContent');

    try {
      const rates = await Api.getExchangeRates() || [];

      let ratesHTML = '';
      rates.forEach(r => {
        ratesHTML += `
          <div class="config-item">
            <div class="config-key">
              ${r.fromCurrency || ''} / ${r.toCurrency || ''}
              <small>Tipo de cambio</small>
            </div>
            <div class="config-value">
              <input type="number" step="0.01" value="${r.rate || 0}" data-from="${r.fromCurrency}" data-to="${r.toCurrency}" class="rate-input" />
              <button class="btn btn-primary btn-sm save-rate-btn">Guardar</button>
            </div>
          </div>
        `;
      });

      if (!ratesHTML) {
        ratesHTML = `
          <div class="config-item">
            <div class="config-key">
              USD / ARS
              <small>Tipo de cambio</small>
            </div>
            <div class="config-value">
              <input type="number" step="0.01" value="${exchangeRate}" data-from="USD" data-to="ARS" class="rate-input" />
              <button class="btn btn-primary btn-sm save-rate-btn">Guardar</button>
            </div>
          </div>
        `;
      }

      page.innerHTML = `
        <div class="view-header">
          <h2>Tipos de Cambio</h2>
        </div>

        <div class="card">
          <div class="card-body">
            <div class="config-grid" id="ratesGrid">
              ${ratesHTML}
            </div>
          </div>
        </div>
      `;

      /* Bind save events */
      document.querySelectorAll('.save-rate-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
          const item = btn.closest('.config-item');
          const input = item.querySelector('.rate-input');
          const data = {
            fromCurrency: input.dataset.from,
            toCurrency: input.dataset.to,
            rate: parseFloat(input.value) || 0
          };
          try {
            await Api.updateExchangeRate(data);
            showToast('Tipo de cambio actualizado');
            await loadExchangeRateData();
          } catch (err) {
            showToast(err.message, 'error');
          }
        });
      });

    } catch (err) {
      page.innerHTML = '<div class="empty-state">' + iconRaw('warning') + '<h4>Error al cargar tipos de cambio</h4><p>' + (err.message || '') + '</p></div>';
    }
  }

  /* ==========================================================
     VIEW: CURRENCIES (Monedas)
     ========================================================== */
  function loadCurrencies() {
    const page = document.getElementById('pageContent');

    page.innerHTML = `
      <div class="view-header">
        <h2>Monedas</h2>
      </div>
      <div class="card">
        <div class="card-body">
          <div class="config-grid">
            <div class="config-item">
              <div class="config-key">
                ARS
                <small>Peso Argentino</small>
              </div>
              <div class="config-value">
                <span class="badge" style="background:var(--primary-bg);color:var(--primary)">Activa</span>
              </div>
            </div>
            <div class="config-item">
              <div class="config-key">
                USD
                <small>Dolar Estadounidense</small>
              </div>
              <div class="config-value">
                <span class="badge" style="background:var(--primary-bg);color:var(--primary)">Activa</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  /* ==========================================================
     VIEW: IMPORT
     ========================================================== */
  function loadImport() {
    const page = document.getElementById('pageContent');
    page.innerHTML = `
      <div class="view-header">
        <h2>Importar Datos</h2>
      </div>
      <div class="card">
        <div class="card-body">
          <div class="empty-state">
            ${iconRaw('upload')}
            <h4>Importar Movimientos</h4>
            <p>Arrastra un archivo CSV o Excel aqui, o haz clic para seleccionar</p>
            <button class="btn btn-primary mt-16">${iconRaw('upload')} Seleccionar Archivo</button>
          </div>
        </div>
      </div>
    `;
  }

  /* ==========================================================
     VIEW: HISTORY
     ========================================================== */
  function loadHistory() {
    const page = document.getElementById('pageContent');
    page.innerHTML = `
      <div class="view-header">
        <h2>Historico</h2>
      </div>
      <div class="card">
        <div class="card-body">
          <div class="empty-state">
            ${iconRaw('history')}
            <h4>Resumen Historico</h4>
            <p>Visualiza tendencias y comparativas de periodos anteriores</p>
          </div>
        </div>
      </div>
    `;
  }

  /* ==========================================================
     VIEW: PLACEHOLDER (for unimplemented config views)
     ========================================================== */
  function loadPlaceholder(title) {
    const page = document.getElementById('pageContent');
    page.innerHTML = `
      <div class="view-header">
        <h2>${title}</h2>
      </div>
      <div class="card">
        <div class="card-body">
          <div class="empty-state">
            ${iconRaw('settings')}
            <h4>${title}</h4>
            <p>Esta seccion estara disponible proximamente</p>
          </div>
        </div>
      </div>
    `;
  }

  /* ==========================================================
     INIT
     ========================================================== */
  async function init() {
    if (!Auth.requireAuth()) return;

    renderShell();
    bindEvents();
    updateMonthLabel();

    /* Remove boot loader */
    const bootLoader = document.getElementById('bootLoader');
    if (bootLoader) bootLoader.remove();

    /* Load reference data in parallel */
    await Promise.all([
      loadAccountsData(),
      loadCategoriesData(),
      loadExchangeRateData()
    ]);

    /* Load initial view */
    loadDashboard();
  }

  /* ==========================================================
     BOOT
     ========================================================== */
  document.addEventListener('DOMContentLoaded', init);

  return { init, navigateTo };
})();
