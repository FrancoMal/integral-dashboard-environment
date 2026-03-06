/* ============================================================
   CashFlow Dashboard - API Client Module
   Fetch wrapper with JWT, typed helpers for financial endpoints
   ============================================================ */

const Api = (() => {
  const BASE_URL = '/api';

  function headers(extra) {
    const h = {
      'Content-Type': 'application/json'
    };
    if (extra) {
      Object.assign(h, extra);
    }
    const authHeader = Auth.getAuthHeader();
    if (authHeader) {
      h['Authorization'] = authHeader;
    }
    return h;
  }

  async function request(method, path, body) {
    const url = BASE_URL + path;
    const opts = { method, headers: headers() };
    if (body !== undefined && body !== null) {
      opts.body = JSON.stringify(body);
    }

    let response;
    try {
      response = await fetch(url, opts);
    } catch (err) {
      throw new Error('Error de red. Verifica tu conexion.');
    }

    if (response.status === 401) {
      Auth.clearSession();
      window.location.href = '/login.html';
      throw new Error('Sesion expirada');
    }

    let data = null;
    const ct = response.headers.get('content-type');
    if (ct && ct.includes('application/json')) {
      data = await response.json();
    }

    if (!response.ok) {
      const msg = (data && data.message) ? data.message : 'Error del servidor (' + response.status + ')';
      throw new Error(msg);
    }

    return data;
  }

  function get(path) { return request('GET', path); }
  function post(path, body) { return request('POST', path, body); }
  function put(path, body) { return request('PUT', path, body); }
  function del(path) { return request('DELETE', path); }

  /* ============================================================
     Dashboard endpoints
     ============================================================ */
  function getDashboardStats(month, year) {
    return get('/dashboard/stats?month=' + month + '&year=' + year);
  }

  function getDashboardAccounts() {
    return get('/dashboard/accounts');
  }

  function getExpensesByCategory(month, year) {
    return get('/dashboard/expenses-by-category?month=' + month + '&year=' + year);
  }

  function getMonthlyTrend(months) {
    return get('/dashboard/monthly-trend?months=' + (months || 12));
  }

  /* ============================================================
     Transactions
     ============================================================ */
  function getTransactions(filters) {
    const params = new URLSearchParams();
    if (filters.dateFrom && filters.dateTo) {
      params.append('dateFrom', filters.dateFrom);
      params.append('dateTo', filters.dateTo);
    } else {
      if (filters.month) params.append('month', filters.month);
      if (filters.year) params.append('year', filters.year);
    }
    if (filters.type) params.append('type', filters.type);
    if (filters.category) params.append('category', filters.category);
    if (filters.search) params.append('search', filters.search);
    if (filters.page) params.append('page', filters.page);
    if (filters.pageSize) params.append('pageSize', filters.pageSize);
    if (filters.account) params.append('account', filters.account);
    return get('/transactions?' + params.toString());
  }

  function createTransaction(data) {
    return post('/transactions', data);
  }

  function updateTransaction(id, data) {
    return put('/transactions/' + id, data);
  }

  function deleteTransaction(id) {
    return del('/transactions/' + id);
  }

  /* ============================================================
     Accounts
     ============================================================ */
  function getAccounts() {
    return get('/accounts');
  }

  function createAccount(data) {
    return post('/accounts', data);
  }

  function updateAccount(id, data) {
    return put('/accounts/' + id, data);
  }

  function deleteAccount(id) {
    return del('/accounts/' + id);
  }

  /* ============================================================
     Categories
     ============================================================ */
  function getCategories() {
    return get('/categories');
  }

  function createCategory(data) {
    return post('/categories', data);
  }

  function updateCategory(id, data) {
    return put('/categories/' + id, data);
  }

  function deleteCategory(id) {
    return del('/categories/' + id);
  }

  /* ============================================================
     Exchange Rates
     ============================================================ */
  function getExchangeRates() {
    return get('/exchange-rates');
  }

  function updateExchangeRate(data) {
    return put('/exchange-rates', data);
  }

  return {
    get, post, put, del,
    getDashboardStats,
    getDashboardAccounts,
    getExpensesByCategory,
    getMonthlyTrend,
    getTransactions,
    createTransaction,
    updateTransaction,
    deleteTransaction,
    getAccounts,
    createAccount,
    updateAccount,
    deleteAccount,
    getCategories,
    createCategory,
    updateCategory,
    deleteCategory,
    getExchangeRates,
    updateExchangeRate
  };
})();
