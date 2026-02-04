// Todo App State
let tasks = [];
let currentFilter = 'all';
let focusedIndex = -1;
let isHelpVisible = false;

// DOM Elements
const taskInput = document.getElementById('taskInput');
const taskList = document.getElementById('taskList');
const emptyState = document.getElementById('emptyState');
const taskCount = document.getElementById('taskCount');
const clearCompleted = document.getElementById('clearCompleted');
const completedCount = document.getElementById('completedCount');
const filterBtns = document.querySelectorAll('.filter-btn');
const keyboardHelp = document.getElementById('keyboardHelp');
const closeHelp = document.getElementById('closeHelp');

// Initialize
function init() {
    loadTasks();
    render();
    setupEventListeners();
    setupOverlay();
}

// LocalStorage
function loadTasks() {
    const saved = localStorage.getItem('keyboard-todo-tasks');
    if (saved) {
        tasks = JSON.parse(saved);
    }
}

function saveTasks() {
    localStorage.setItem('keyboard-todo-tasks', JSON.stringify(tasks));
}

// Task Management
function addTask(text) {
    const task = {
        id: Date.now(),
        text: text.trim(),
        completed: false,
        createdAt: Date.now()
    };
    tasks.unshift(task);
    saveTasks();
    render();
}

function toggleTask(id) {
    const task = tasks.find(t => t.id === id);
    if (task) {
        task.completed = !task.completed;
        saveTasks();
        render();
    }
}

function deleteTask(id) {
    const item = document.querySelector(`[data-id="${id}"]`);
    if (item) {
        item.classList.add('removing');
        setTimeout(() => {
            tasks = tasks.filter(t => t.id !== id);
            saveTasks();
            render();
        }, 200);
    }
}

function updateTask(id, newText) {
    const task = tasks.find(t => t.id === id);
    if (task && newText.trim()) {
        task.text = newText.trim();
        saveTasks();
        render();
    }
}

function clearCompletedTasks() {
    tasks = tasks.filter(t => !t.completed);
    saveTasks();
    render();
}

// Rendering
function render() {
    const filteredTasks = getFilteredTasks();
    
    // Show/hide empty state
    if (filteredTasks.length === 0) {
        taskList.innerHTML = '';
        emptyState.classList.remove('hidden');
    } else {
        emptyState.classList.add('hidden');
        renderTaskList(filteredTasks);
    }
    
    // Update stats
    updateStats();
    
    // Update filter buttons
    filterBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.filter === currentFilter);
        btn.setAttribute('aria-selected', btn.dataset.filter === currentFilter);
        btn.setAttribute('tabindex', btn.dataset.filter === currentFilter ? '0' : '-1');
    });
    
    // Restore focus if needed
    if (focusedIndex >= 0 && focusedIndex < filteredTasks.length) {
        const items = taskList.querySelectorAll('.task-item');
        if (items[focusedIndex]) {
            items[focusedIndex].focus();
        }
    }
}

function getFilteredTasks() {
    switch (currentFilter) {
        case 'active':
            return tasks.filter(t => !t.completed);
        case 'completed':
            return tasks.filter(t => t.completed);
        default:
            return tasks;
    }
}

function renderTaskList(filteredTasks) {
    taskList.innerHTML = filteredTasks.map((task, index) => `
        <li 
            class="task-item ${task.completed ? 'completed' : ''}" 
            data-id="${task.id}"
            data-index="${index}"
            role="option"
            aria-selected="false"
            tabindex="0"
        >
            <div class="task-checkbox" aria-label="${task.completed ? 'Mark as incomplete' : 'Mark as complete'}">
                <svg viewBox="0 0 24 24">
                    <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
            </div>
            <span class="task-text">${escapeHtml(task.text)}</span>
            <input type="text" class="task-edit-input" value="${escapeHtml(task.text)}" style="display: none;">
            <div class="task-actions">
                <button class="task-btn edit" data-action="edit" aria-label="Edit task">✏️</button>
                <button class="task-btn delete" data-action="delete" aria-label="Delete task">🗑️</button>
            </div>
        </li>
    `).join('');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function updateStats() {
    const activeCount = tasks.filter(t => !t.completed).length;
    const completedTasks = tasks.filter(t => t.completed).length;
    
    taskCount.textContent = `${activeCount} item${activeCount !== 1 ? 's' : ''} left`;
    
    if (completedTasks > 0) {
        clearCompleted.classList.remove('hidden');
        completedCount.textContent = completedTasks;
    } else {
        clearCompleted.classList.add('hidden');
    }
}

// Event Listeners
function setupEventListeners() {
    // Input field
    taskInput.addEventListener('keydown', handleInputKeydown);
    
    // Global keyboard shortcuts
    document.addEventListener('keydown', handleGlobalKeydown);
    
    // Filter buttons
    filterBtns.forEach(btn => {
        btn.addEventListener('click', () => setFilter(btn.dataset.filter));
    });
    
    // Clear completed
    clearCompleted.addEventListener('click', clearCompletedTasks);
    
    // Task list delegation
    taskList.addEventListener('click', handleTaskClick);
    taskList.addEventListener('keydown', handleTaskKeydown);
    
    // Help modal
    closeHelp.addEventListener('click', hideHelp);
}

function setupOverlay() {
    const overlay = document.createElement('div');
    overlay.className = 'overlay';
    overlay.id = 'overlay';
    document.body.appendChild(overlay);
    overlay.addEventListener('click', hideHelp);
}

// Keyboard Handlers
function handleInputKeydown(e) {
    if (e.key === 'Enter' && taskInput.value.trim()) {
        addTask(taskInput.value);
        taskInput.value = '';
        e.preventDefault();
    }
}

function handleGlobalKeydown(e) {
    // Don't trigger shortcuts when editing
    if (document.querySelector('.task-item.editing')) {
        if (e.key === 'Escape') {
            cancelEdit();
        }
        return;
    }
    
    // Don't trigger when typing in input
    if (document.activeElement === taskInput) {
        if (e.key === 'Escape') {
            taskInput.blur();
        }
        return;
    }
    
    switch (e.key) {
        case '/':
            e.preventDefault();
            taskInput.focus();
            break;
            
        case '?':
            e.preventDefault();
            toggleHelp();
            break;
            
        case '1':
            e.preventDefault();
            setFilter('all');
            break;
            
        case '2':
            e.preventDefault();
            setFilter('active');
            break;
            
        case '3':
            e.preventDefault();
            setFilter('completed');
            break;
            
        case 'Escape':
            if (isHelpVisible) {
                hideHelp();
            } else if (document.activeElement !== taskInput) {
                taskInput.focus();
            }
            break;
    }
}

function handleTaskKeydown(e) {
    const item = e.target.closest('.task-item');
    if (!item) return;
    
    const id = parseInt(item.dataset.id);
    const items = Array.from(taskList.querySelectorAll('.task-item'));
    const currentIndex = items.indexOf(item);
    
    switch (e.key) {
        case 'ArrowDown':
            e.preventDefault();
            if (currentIndex < items.length - 1) {
                items[currentIndex + 1].focus();
                focusedIndex = currentIndex + 1;
            }
            break;
            
        case 'ArrowUp':
            e.preventDefault();
            if (currentIndex > 0) {
                items[currentIndex - 1].focus();
                focusedIndex = currentIndex - 1;
            } else {
                taskInput.focus();
                focusedIndex = -1;
            }
            break;
            
        case 'Enter':
        case ' ':
            e.preventDefault();
            toggleTask(id);
            break;
            
        case 'd':
        case 'D':
        case 'Delete':
            e.preventDefault();
            deleteTask(id);
            // Focus next item or previous
            setTimeout(() => {
                const newItems = taskList.querySelectorAll('.task-item');
                if (newItems.length > 0) {
                    const nextIndex = Math.min(currentIndex, newItems.length - 1);
                    newItems[nextIndex]?.focus();
                } else {
                    taskInput.focus();
                }
            }, 250);
            break;
            
        case 'e':
        case 'E':
            e.preventDefault();
            startEdit(item, id);
            break;
            
        case 'Home':
            e.preventDefault();
            items[0]?.focus();
            break;
            
        case 'End':
            e.preventDefault();
            items[items.length - 1]?.focus();
            break;
    }
}

function handleTaskClick(e) {
    const item = e.target.closest('.task-item');
    if (!item) return;
    
    const id = parseInt(item.dataset.id);
    const action = e.target.closest('[data-action]')?.dataset.action;
    
    if (action === 'delete') {
        deleteTask(id);
    } else if (action === 'edit') {
        startEdit(item, id);
    } else if (e.target.closest('.task-checkbox')) {
        toggleTask(id);
    }
}

// Edit Mode
function startEdit(item, id) {
    item.classList.add('editing');
    const input = item.querySelector('.task-edit-input');
    const text = item.querySelector('.task-text');
    
    input.style.display = 'block';
    input.focus();
    input.select();
    
    function save() {
        updateTask(id, input.value);
    }
    
    function cancel() {
        cancelEdit();
    }
    
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            save();
        } else if (e.key === 'Escape') {
            cancel();
        }
    });
    
    input.addEventListener('blur', save);
}

function cancelEdit() {
    render();
}

// Filter
function setFilter(filter) {
    currentFilter = filter;
    focusedIndex = -1;
    render();
}

// Help Modal
function toggleHelp() {
    if (isHelpVisible) {
        hideHelp();
    } else {
        showHelp();
    }
}

function showHelp() {
    isHelpVisible = true;
    keyboardHelp.classList.add('visible');
    document.getElementById('overlay').classList.add('visible');
    closeHelp.focus();
}

function hideHelp() {
    isHelpVisible = false;
    keyboardHelp.classList.remove('visible');
    document.getElementById('overlay').classList.remove('visible');
}

// Start the app
init();
