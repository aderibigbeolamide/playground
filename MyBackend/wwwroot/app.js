const API_URL = '/api/todos';

// DOM Elements
const todoForm = document.getElementById('todo-form');
const taskTitleInput = document.getElementById('task-title');
const taskCompletedInput = document.getElementById('task-completed');
const todoList = document.getElementById('todo-list');
const submitBtn = document.getElementById('submit-btn');
const formSpinner = document.getElementById('form-spinner');
const taskCount = document.getElementById('task-count');
const filterBtns = document.querySelectorAll('.filter-btn');
const connectionBadge = document.getElementById('connection-badge');
const badgeText = document.getElementById('badge-text');
const dbErrorAlert = document.getElementById('db-error-alert');
const emptyState = document.getElementById('empty-state');
const loadingState = document.getElementById('loading-state');
const toastContainer = document.getElementById('toast-container');

// State
let todos = [];
let currentFilter = 'all';
let isDbConnected = true;

// Initialize Application
document.addEventListener('DOMContentLoaded', () => {
    fetchTodos();
    setupEventListeners();
});

// Event Listeners Setup
function setupEventListeners() {
    // Form Submit
    todoForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const title = taskTitleInput.value.trim();
        const isCompleted = taskCompletedInput.checked;
        
        if (!title) return;
        
        await createTodo({ title, isCompleted });
    });

    // Filter Buttons
    filterBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            filterBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentFilter = btn.dataset.filter;
            renderTodos();
        });
    });
}

// Fetch all Todos from API
async function fetchTodos() {
    showLoading(true);
    try {
        const response = await fetch(API_URL);
        if (!response.ok) throw new Error('API response failed');
        
        todos = await response.json();
        setConnectionStatus(true);
        showDbAlert(false);
    } catch (error) {
        console.error('Error fetching todos:', error);
        setConnectionStatus(false);
        showDbAlert(true);
        showToast('Failed to fetch tasks from database', 'error');
    } finally {
        showLoading(false);
        renderTodos();
    }
}

// Create a new Todo item
async function createTodo(todoData) {
    setFormLoading(true);
    try {
        const response = await fetch(API_URL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'accept': '*/*'
            },
            body: JSON.stringify(todoData)
        });

        if (!response.ok) throw new Error('Failed to create todo');

        const newTodo = await response.json();
        todos.unshift(newTodo); // Add to the beginning of list
        
        // Reset Form
        taskTitleInput.value = '';
        taskCompletedInput.checked = false;
        
        showToast('Task added successfully!', 'success');
        renderTodos();
        showDbAlert(false);
    } catch (error) {
        console.error('Error creating todo:', error);
        showToast('Failed to create task. Check database connection.', 'error');
        showDbAlert(true);
    } finally {
        setFormLoading(false);
    }
}

// Toggle Completed Status
async function toggleTodo(todo) {
    const updatedTodo = {
        ...todo,
        isCompleted: !todo.isCompleted
    };

    try {
        const response = await fetch(`${API_URL}/${todo.id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedTodo)
        });

        if (!response.ok) throw new Error('Failed to update todo');

        // Update local state
        todo.isCompleted = updatedTodo.isCompleted;
        
        showToast(`Task marked as ${todo.isCompleted ? 'completed' : 'active'}`, 'success');
        renderTodos();
        showDbAlert(false);
    } catch (error) {
        console.error('Error updating todo:', error);
        showToast('Failed to update task status.', 'error');
        showDbAlert(true);
    }
}

// Delete a Todo Item
async function deleteTodoItem(id) {
    try {
        const response = await fetch(`${API_URL}/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) throw new Error('Failed to delete todo');

        // Remove from local state
        todos = todos.filter(t => t.id !== id);
        
        showToast('Task deleted successfully', 'success');
        renderTodos();
        showDbAlert(false);
    } catch (error) {
        console.error('Error deleting todo:', error);
        showToast('Failed to delete task.', 'error');
        showDbAlert(true);
    }
}

// Render Todos list in DOM based on current filter
function renderTodos() {
    todoList.innerHTML = '';
    
    // Apply filter
    const filteredTodos = todos.filter(todo => {
        if (currentFilter === 'active') return !todo.isCompleted;
        if (currentFilter === 'completed') return todo.isCompleted;
        return true; // 'all'
    });

    // Update Counter
    taskCount.textContent = filteredTodos.length;

    // Show empty state if needed
    if (filteredTodos.length === 0 && !loadingState.classList.contains('hidden')) {
        emptyState.classList.add('hidden');
    } else if (filteredTodos.length === 0) {
        emptyState.classList.remove('hidden');
    } else {
        emptyState.classList.add('hidden');
    }

    // Build DOM elements
    filteredTodos.forEach((todo, index) => {
        const li = document.createElement('li');
        li.className = `todo-item ${todo.isCompleted ? 'completed' : ''}`;
        li.style.animation = `slide-up 0.4s cubic-bezier(0.16, 1, 0.3, 1) forwards`;
        li.style.animationDelay = `${index * 0.05}s`;
        
        const dateStr = todo.createdAt ? new Date(todo.createdAt).toLocaleString() : 'Just now';
        
        li.innerHTML = `
            <div class="todo-item-left">
                <label class="checkbox-container">
                    <input type="checkbox" ${todo.isCompleted ? 'checked' : ''} class="toggle-checkbox">
                    <span class="checkmark"></span>
                </label>
                <div class="todo-item-meta">
                    <span class="todo-title">${escapeHtml(todo.title)}</span>
                    <span class="todo-date">${dateStr}</span>
                </div>
            </div>
            <button class="btn-delete" title="Delete task">
                <i data-lucide="trash-2"></i>
            </button>
        `;

        // Event handlers for new elements
        const checkbox = li.querySelector('.toggle-checkbox');
        checkbox.addEventListener('change', () => toggleTodo(todo));

        const deleteBtn = li.querySelector('.btn-delete');
        deleteBtn.addEventListener('click', () => deleteTodoItem(todo.id));

        todoList.appendChild(li);
    });

    // Reinitialize Lucide Icons for dynamic content
    lucide.createIcons();
}

// Helpers & UI States
function showLoading(isLoading) {
    if (isLoading) {
        loadingState.classList.remove('hidden');
        todoList.classList.add('hidden');
        emptyState.classList.add('hidden');
    } else {
        loadingState.classList.add('hidden');
        todoList.classList.remove('hidden');
    }
}

function setFormLoading(isLoading) {
    if (isLoading) {
        submitBtn.disabled = true;
        formSpinner.classList.remove('hidden');
        submitBtn.querySelector('i').classList.add('hidden');
    } else {
        submitBtn.disabled = false;
        formSpinner.classList.add('hidden');
        submitBtn.querySelector('i').classList.remove('hidden');
    }
}

function setConnectionStatus(isConnected) {
    connectionBadge.className = 'badge';
    if (isConnected) {
        connectionBadge.classList.add('badge-connected');
        badgeText.textContent = 'API Connected';
    } else {
        connectionBadge.classList.add('badge-disconnected');
        badgeText.textContent = 'Connection Error';
    }
}

function showDbAlert(isError) {
    if (isError) {
        dbErrorAlert.classList.remove('hidden');
    } else {
        dbErrorAlert.classList.add('hidden');
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

    // Auto remove toast
    setTimeout(() => {
        toast.style.animation = 'fade-in 0.3s ease reverse forwards';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Prevent HTML injections
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}
