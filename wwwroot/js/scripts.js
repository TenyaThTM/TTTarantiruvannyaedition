
document.addEventListener('DOMContentLoaded', function() {
    console.log('Game script loaded');
    
    // АВТОМАТИЧЕСКИ СОЗДАЕМ ИГРОКА ПРИ ЗАГРУЗКЕ
    getMySessionId().then(async () => {
        await ensurePlayerExists(); // ПЕРЕМЕЩАЕМ СЮДА - сначала создаем игрока
        await loadPlayer(); // потом загружаем данные
        showScreen('main');
    }).catch(error => {
        console.error('Initialization error:', error);
        showScreen('main');
    });
});
    let mySessionId = '';
    // Элементы DOM
    const mainScreen = document.getElementById('mainScreen');
    const lobbiesScreen = document.getElementById('lobbiesScreen');
    const lobbyScreen = document.getElementById('lobbyScreen');
    const gameScreen = document.getElementById('gameScreen');
    
    const nInput = document.getElementById('n');
    const bInput = document.getElementById('b');
    const dInput = document.getElementById('d');
    const tInput = document.getElementById('t');
    const uInput = document.getElementById('u');
    const createLobbyBtn = document.getElementById('createLobby');
    const showLobbiesBtn = document.getElementById('showLobbies');
    const backToMainBtn = document.getElementById('backToMain');
    const startGameBtn = document.getElementById('startGame');
    const leaveLobbyBtn = document.getElementById('leaveLobby');
    const boardElement = document.getElementById('board');
    const statusElement = document.getElementById('status');
    const tokenElements = document.querySelectorAll('.token');
    const playersListElement = document.getElementById('players');
    const gamePlayersElement = document.getElementById('gamePlayers');
    const copyUrlButton = document.getElementById('copyUrl');
    const lobbiesList = document.getElementById('lobbiesList');
    const startGameSection = document.getElementById('startGameSection');
    const lobbyIdDisplay = document.getElementById('lobbyIdDisplay');
    const profileModal = document.getElementById('profileModal');
const openProfileBtn = document.getElementById('openProfile');
const closeProfileModalBtn = document.getElementById('closeProfileModal');
const saveProfileBtn = document.getElementById('saveProfile');
const playerNameInput = document.getElementById('playerName');
const avatarUpload = document.getElementById('avatarUpload');
const avatarPreview = document.getElementById('avatarPreview');
const gameIdDisplay = document.getElementById('gameIdDisplay');

    let selectedColor = null;
    let currentLobbyId = null;
    let currentGameId = null;
    let myColor = null;
    let isLobbyCreator = false;
    let pollInterval = null;
    let currentProfile = null;
    let isPageClosing = false;
    let currentPlayer = null;
    let lastGameState = null;

    // Генерируем базовый URL
    const baseUrl = window.location.origin;
    
    // Обработчики выбора токена
    tokenElements.forEach(token => {
        token.addEventListener('click', () => {
            tokenElements.forEach(t => t.classList.remove('selected'));
            token.classList.add('selected');
            selectedColor = token.getAttribute('data-color');
            console.log('Selected color:', selectedColor);
        });
    });
    document.getElementById('checkGame').addEventListener('click', async () => {
    const gameStarted = await checkGameStarted();
    if (!gameStarted) {
        alert('Игра еще не началась!');
    }
});


async function getMySessionId() {
    try {
        // Получаем sessionId с сервера
        const response = await fetch('/Game/GetSessionId');
        const result = await response.json();
        if (result.success) {
            mySessionId = result.sessionId;
            console.log('My session ID:', mySessionId);
        }
    } catch (error) {
        console.error('Error getting session ID:', error);
        // Fallback: пытаемся получить из cookies
        mySessionId = document.cookie.match(/\.AspNetCore\.Session=([^;]+)/)?.[1] || '';
    }
    return mySessionId;
}
window.addEventListener('beforeunload', async (e) => {
    if (isPageClosing) return; // Предотвращаем множественные вызовы
    
    isPageClosing = true;
    
    // Пытаемся выйти из лобби/игры при закрытии
    try {
        if (currentLobbyId || currentGameId) {
            // Используем sendBeacon для надежной отправки при закрытии
            const data = new Blob([JSON.stringify({})], {type: 'application/json'});
            navigator.sendBeacon('/Game/LeaveLobby', data);
        }
    } catch (error) {
        console.error('Error during page unload:', error);
    }
});
window.addEventListener('pageshow', (e) => {
    if (e.persisted) {
        isPageClosing = false;
    }
});
// Your code appears to be missing some context, but here's the corrected structure
async function checkGameState() {
    try {
        console.log('Manually checking game state for game:', currentGameId);
        
        const response = await fetch('/Game/GameState');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const gameData = await response.json();
        console.log('Manual game check result:', gameData);
        
        if (gameData.error) {
            console.error('Game error in manual check:', gameData.error);
            
            if (gameData.error.includes("not found") || gameData.error.includes("not in this game")) {
                // Проверяем лобби
                const lobbyResponse = await fetch('/Game/LobbyState');
                const lobbyData = await lobbyResponse.json();
                
                if (!lobbyData.error && lobbyData.status === 'GameStarted') {
                    console.log('Game exists, updating...');
                    currentGameId = lobbyData.gameId;
                    gameIdDisplay.textContent = currentGameId;
                    
                    // Обновляем сессию
                    await fetch('/Game/UpdateGameSession', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ gameId: currentGameId })
                    });
                    
                    // Перезапускаем опрос
                    if (pollInterval) clearInterval(pollInterval);
                    startGamePolling();
                } else {
                    console.log('Returning to lobby');
                    showScreen('lobby');
                    startLobbyPolling();
                }
            }
            return;
        }
        
        // Обновляем UI
        updateBoard(gameData);
        updateStatus(gameData);
        updateGamePlayersList(gameData.players || []);
        
    } catch (error) {
        console.error('Error in manual game check:', error);
    }
}

async function savePlayer() {
    const formData = new FormData();
    formData.append('name', playerNameInput.value);
    formData.append('color', selectedColor); // Сохраняем выбранный цвет
    
    if (avatarUpload.files[0]) {
        formData.append('avatar', avatarUpload.files[0]);
    }
    
    try {
        const response = await fetch('/api/Player', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        if (result.success) {
            currentPlayer = result.player;
            profileModal.style.display = 'none';
            alert('Профиль сохранен!');
        }
    } catch (error) {
        console.error('Ошибка сохранения игрока:', error);
    }
}
async function ensurePlayerExists() {
    try {
        const response = await fetch('/api/Player');
        const result = await response.json();
        
        // Если игрок уже существует, обновляем его данные
        if (result.success && result.player) {
            currentPlayer = result.player;
            
            // Обновляем цвет, если он выбран
            if (selectedColor && result.player.color !== selectedColor) {
                await fetch('/api/Player', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `name=${encodeURIComponent(result.player.name)}&color=${encodeURIComponent(selectedColor)}`
                });
            }
            return true;
        }
        
        // Создаем нового игрока с выбранным цветом
        const playerName = playerNameInput?.value || 'Игрок';
        const quickResponse = await fetch('/api/Player/quickCreate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                Name: playerName,
                Color: selectedColor || '#FF5252'
            })
        });
        
        const quickResult = await quickResponse.json();
        
        if (quickResult.success) {
            currentPlayer = quickResult.player;
            // Обновляем поле имени если нужно
            if (playerNameInput && !playerNameInput.value) {
                playerNameInput.value = playerName;
            }
            return true;
        }
        
        return false;
        
    } catch (error) {
        console.error('Error ensuring player exists:', error);
        return false;
    }
}
async function loadPlayer() {
    try {
        const response = await fetch('/api/Player');
        const result = await response.json();
        
        if (result.success && result.player) {
            currentPlayer = result.player;
            if (playerNameInput) {
                playerNameInput.value = currentPlayer.name;
            }
            
            if (currentPlayer.avatarPath) {
                avatarPreview.innerHTML = `<img src="${currentPlayer.avatarPath}" alt="Аватар">`;
            }
            
            // Автоматически выбираем цвет игрока если он есть
            if (currentPlayer.color && !selectedColor) {
                selectedColor = currentPlayer.color;
                // Находим и выделяем соответствующий токен
                tokenElements.forEach(token => {
    token.addEventListener('click', async () => {
        tokenElements.forEach(t => t.classList.remove('selected'));
        token.classList.add('selected');
        selectedColor = token.getAttribute('data-color');
        console.log('Selected color:', selectedColor);
        
        // Автоматически обновляем цвет игрока при выборе
        if (currentPlayer) {
            await ensurePlayerExists();
        }
    });
});
            }
        }
    } catch (error) {
        console.error('Ошибка загрузки игрока:', error);
    }
}
// Обработчики модального окна
// Исправляем обработчик открытия профиля
openProfileBtn.addEventListener('click', async () => {
    // Убеждаемся что игрок существует перед открытием модального окна
    const playerExists = await ensurePlayerExists();
    if (playerExists) {
        await loadPlayer();
        profileModal.style.display = 'block';
    } else {
        alert('Ошибка загрузки профиля');
    }
});

closeProfileModalBtn.addEventListener('click', () => {
    profileModal.style.display = 'none';
});

saveProfileBtn.addEventListener('click', savePlayer);

// Закрытие модального окна по клику вне его
window.addEventListener('click', (e) => {
    if (e.target === profileModal) {
        profileModal.style.display = 'none';
    }
});
    // Показать экран
    function showScreen(screenName) {
        console.log('Showing screen:', screenName);
        
        // Скрыть все экраны
        mainScreen.style.display = 'none';
        lobbiesScreen.style.display = 'none';
        lobbyScreen.style.display = 'none';
        gameScreen.style.display = 'none';
        
        // Показать нужный экран
        switch(screenName) {
            case 'main':
                mainScreen.style.display = 'block';
                statusElement.textContent = 'Выберите цвет и нажмите "Создать лобби" или "Присоединиться"';
                break;
            case 'lobbies':
                lobbiesScreen.style.display = 'block';
                break;
            case 'lobby':
                lobbyScreen.style.display = 'block';
                if (currentLobbyId) {
                    lobbyIdDisplay.textContent = currentLobbyId;
                }
                break;
            case 'game':
                gameScreen.style.display = 'block';
                boardElement.style.display = 'grid';
                break;
        }
    }
    startGameBtn.addEventListener('click', async () => {
    try {
        console.log('Starting game...');
        const response = await fetch('/Game/StartGame', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ LobbyId: currentLobbyId })
        });
        
        const result = await response.json();
        console.log('Start game result:', result);
        
        if (result.success) {
            currentGameId = result.gameId;
            statusElement.textContent = 'Игра начинается! Уведомляем других игроков...';
            
            // Даем время другим игрокам получить уведомление
            setTimeout(() => {
                showScreen('game');
                startGamePolling();
            }, 1000);
        } else {
            alert('Не удалось начать игру: ' + result.error);
        }
    } catch (error) {
        console.error('Ошибка начала игры:', error);
        alert('Ошибка при запуске игры');
    }
});
    // Создание лобби
    createLobbyBtn.addEventListener('click', async () => {
    await getMySessionId();
    if (!selectedColor) {
        alert('Пожалуйста, выберите цвет для игры!');
        return;
    }
    
    try {
        // Сначала убедимся что игрок существует с правильными данными
        const playerCreated = await ensurePlayerExists();
        if (!playerCreated) {
            alert('Ошибка создания игрока');
            return;
        }
        
        console.log('Creating lobby...');
        const params = {
            Width: parseInt(nInput.value),
            Height: parseInt(bInput.value),
            BlockChance: parseInt(dInput.value),
            WinLength: parseInt(tInput.value),
            CaptureProgress: parseInt(uInput.value)
        };
        
        const response = await fetch('/Game/CreateLobby', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(params)
        });
        
        const result = await response.json();
        console.log('Create lobby result:', result);
        
        if (!result.success) {
            alert('Ошибка создания лобби: ' + result.error);
            return;
        }
            
        currentLobbyId = result.lobbyId;
        myColor = selectedColor;
        isLobbyCreator = true;
        
        showScreen('lobby');
        statusElement.textContent = 'Лобби создано! Ожидаем игроков...';
        startGameSection.style.display = 'block';
        
        startLobbyPolling();
        
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Произошла ошибка при создании лобби');
    }
});
    
    // Показать список лобби
    showLobbiesBtn.addEventListener('click', async () => {
        try {
            console.log('Loading lobbies...');
            const response = await fetch('/Game/ListLobbies');
            const lobbies = await response.json();
            console.log('Lobbies:', lobbies);
            
            lobbiesList.innerHTML = '';
            
            if (lobbies.length === 0) {
                lobbiesList.innerHTML = '<p>Нет доступных лобби</p>';
            } else {
                lobbies.forEach(lobby => {
                    const lobbyItem = document.createElement('div');
                    lobbyItem.className = 'lobby-item';
                    lobbyItem.innerHTML = `
                        <div class="lobby-info-display">
                            <span>Лобби #${lobby.id} (${lobby.width}x${lobby.height})</span>
                            <span class="lobby-players">${lobby.playerCount}/6 игроков</span>
                        </div>
                        <div>Победа: ${lobby.winLength} в ряд, Захват: ${lobby.captureProgress} хода</div>
                    `;
                    
                    lobbyItem.addEventListener('click', () => joinExistingLobby(lobby.id));
                    lobbiesList.appendChild(lobbyItem);
                });
            }
            
            showScreen('lobbies');
        } catch (error) {
            console.error('Ошибка получения списка лобби:', error);
        }
    });
    
    // Назад к главному меню
    backToMainBtn.addEventListener('click', () => {
        console.log('Returning to main menu');
        showScreen('main');
    });
    
    // Присоединение к существующему лобби

    async function joinExistingLobby(lobbyId) {
    try {
        console.log('Joining existing lobby:', lobbyId);
        
        if (!selectedColor) {
            alert('Пожалуйста, выберите цвет перед присоединением!');
            return;
        }

        await ensurePlayerExists();
        
        const playerResponse = await fetch('/api/Player');
        const playerResult = await playerResponse.json();
        const playerName = (playerResult.success && playerResult.player) 
            ? playerResult.player.name 
            : 'Игрок';

        const joinResponse = await fetch('/Game/JoinLobby', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                LobbyId: lobbyId,
                Color: selectedColor,
                Name: playerName
            })
        });
        
        const joinResult = await joinResponse.json();
        console.log('Join result:', joinResult);
        
        if (joinResult.success) {
    currentLobbyId = lobbyId;
    isLobbyCreator = false;
    
    showScreen('lobby');
    statusElement.textContent = `Присоединились к лобби! Игроков: ${joinResult.lobby.playerCount}/6`;
    startGameSection.style.display = 'none';
    
    startLobbyPolling();
} else {
            // Показываем конкретную ошибку от сервера
            alert('Ошибка присоединения: ' + (joinResult.error || 'Неизвестная ошибка'));
            // Возвращаем к списку лобби при ошибке
            showScreen('lobbies');
            await showLobbiesBtn.click(); // Обновляем список
        }
    } catch (error) {
        console.error('Ошибка присоединения к лобби:', error);
        alert('Произошла ошибка при присоединении к лобби');
    }
}
    
    // Обновление состояния лобби
    function updateLobbyState(lobbyData) {
    if (lobbyData.playerCount !== undefined) {
        statusElement.textContent = `Ожидаем игроков... (${lobbyData.playerCount}/6)`;
    }
}
    
    // Запуск игры
    startGameBtn.addEventListener('click', async () => {
    try {
        console.log('Starting game...');
        const response = await fetch('/Game/StartGame', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ LobbyId: currentLobbyId })
        });
        
        const result = await response.json();
        console.log('Start game result:', result);
        
        if (result.success) {
            currentGameId = result.gameId;
            statusElement.textContent = 'Игра начинается! Уведомляем других игроков...';
            
            // Даем время другим игрокам получить уведомление
            setTimeout(() => {
                showScreen('game');
                startGamePolling();
            }, 1000);
        } else {
            // ПОКАЗЫВАЕМ ОШИБКУ ТОЛЬКО ЕСЛИ ЭТО НЕ СТАНДАРТНАЯ ПРОВЕРКА НА 2 ИГРОКА
            if (result.error && !result.error.includes("Need at least 2 players")) {
                alert('Не удалось начать игру: ' + result.error);
            } else if (result.error) {
                // Для ошибки "недостаточно игроков" показываем информативное сообщение
                statusElement.textContent = 'Ожидаем второго игрока... (1/2)';
            }
        }
    } catch (error) {
        console.error('Ошибка начала игры:', error);
        alert('Ошибка при запуске игры');
    }
});
    
    // Выйти из лобби
    leaveLobbyBtn.addEventListener('click', async () => {
        try {
            // Очищаем сессию на сервере
            const response = await fetch('/Game/LeaveLobby', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            
            // Сбрасываем локальное состояние
            currentLobbyId = null;
            currentGameId = null;
            myColor = null;
            isLobbyCreator = false;
            
            if (pollInterval) clearInterval(pollInterval);
            
            showScreen('main');
            statusElement.textContent = 'Выберите цвет и нажмите "Создать лобби" или "Присоединиться"';
            
        } catch (error) {
            console.error('Ошибка выхода из лобби:', error);
            // Все равно переходим на главный экран
            showScreen('main');
        }
    });
    async function checkGameStarted() {
    try {
        console.log('Force checking if game started...');
        const response = await fetch('/Game/CheckGameStarted');
        const result = await response.json();
        
        if (result.gameStarted) {
            console.log('Game has started! Switching to game screen');
            currentGameId = result.gameId;
            clearInterval(pollInterval);
            showScreen('game');
            startGamePolling();
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error checking game start:', error);
        return false;
    }
}
// Добавьте эту функцию в scripts.js
function updateStartGameButton(lobbyData) {
    if (!lobbyData || !lobbyData.players) return;
    
    const playerCount = lobbyData.players.length;
    const canStartGame = playerCount >= 2;
    
    if (startGameBtn) {
        startGameBtn.disabled = !canStartGame;
        
        if (!canStartGame) {
            startGameBtn.title = `Нужно минимум 2 игрока (сейчас: ${playerCount})`;
            startGameBtn.style.opacity = '0.6';
        } else {
            startGameBtn.title = 'Начать игру';
            startGameBtn.style.opacity = '1';
        }
    }
    
    // Обновляем статус
    if (statusElement) {
        if (playerCount < 2) {
            statusElement.textContent = `Ожидаем игроков... (${playerCount}/2)`;
        } else {
            statusElement.textContent = `Готово к началу! (${playerCount}/6)`;
        }
    }
}
    // Опрос состояния лобби
function startLobbyPolling() {
    if (pollInterval) clearInterval(pollInterval);
    
    console.log('Starting lobby polling for lobby:', currentLobbyId);
    
    pollInterval = setInterval(async () => {
        try {
            console.log('Polling lobby state...');
            const response = await fetch('/Game/LobbyState');
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const lobbyData = await response.json();
            console.log('Lobby poll result:', lobbyData);
            
            if (lobbyData.error) {
                console.error('Lobby error:', lobbyData.error);
                
                if (lobbyData.error.includes("not found") || 
                    lobbyData.error.includes("No lobby") ||
                    lobbyData.error.includes("not in this lobby")) {
                    
                    // Пытаемся переподключиться
                    console.log('Attempting to reconnect to lobby...');
                    const reconnected = await reconnectToLobby();
                    if (!reconnected) {
                        statusElement.textContent = "Лобби было закрыто";
                        clearInterval(pollInterval);
                        showScreen('main');
                    }
                }
                return;
            }
            
            // Обновляем список игроков
            updatePlayersList(lobbyData.players || []);
            
            // Если игра началась
            if (lobbyData.status === 'GameStarted' && lobbyData.gameId) {
    console.log('Game started! Switching to game screen');
    currentGameId = lobbyData.gameId;
    clearInterval(pollInterval);
    showScreen('game');
    startGamePolling();
    updateStartGameButton(lobbyData);
    
    // ОБНОВЛЯЕМ СЕССИЮ НА СЕРВЕРЕ
    await fetch('/Game/UpdateGameSession', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ gameId: currentGameId })
    });
}
        } catch (error) {
            console.error('Ошибка опроса лобби:', error);
        }
    }, 3000); // Увеличиваем интервал до 3 секунд
}

// Функция переподключения к лобби
async function reconnectToLobby() {
    if (!currentLobbyId) return false;
    
    try {
        const response = await fetch('/Game/JoinLobby', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ LobbyId: currentLobbyId })
        });
        
        const result = await response.json();
        return result.success;
    } catch (error) {
        console.error('Reconnection error:', error);
        return false;
    }
}
    

    // Опрос состояния игры
function startGamePolling() {
    if (pollInterval) clearInterval(pollInterval);
    
    console.log('Starting game polling for game:', currentGameId);
    
    // Немедленно делаем первый запрос
    fetchGameState();
    
    // Затем запускаем интервал
    pollInterval = setInterval(fetchGameState, 2000);
}
async function fetchWithTimeout(url, options = {}, timeout = 5000) {
    const controller = new AbortController();
    const id = setTimeout(() => controller.abort(), timeout);
    
    try {
        const response = await fetch(url, {
            ...options,
            signal: controller.signal
        });
        clearTimeout(id);
        return response;
    } catch (error) {
        clearTimeout(id);
        throw error;
    }
}
async function fetchGameState() {
    try {
        console.log('Polling game state...');
        const response = await fetchWithTimeout('/Game/GameState', {}, 3000);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const gameData = await response.json();
        
        // Проверяем, изменилось ли состояние игры
        const gameStateChanged = JSON.stringify(gameData) !== JSON.stringify(lastGameState);
        
        if (gameData.error) {
            await handleGameError(gameData.error, gameData.redirectToLobby);
            return;
        }
        
        // Обновляем UI только если состояние изменилось
        if (gameStateChanged) {
            updateBoard(gameData);
            updateStatus(gameData);
            updateGamePlayersList(gameData.players || [], gameData.currentPlayerIndex || 0);
            lastGameState = gameData;
        }
        
    } catch (error) {
        console.error('Ошибка опроса игры:', error);
        if (error.name === 'AbortError') {
            console.log('Game request timeout');
            updateConnectionStatus(false);
        }
        await handleGameError(error.message, false);
    }
}
async function handleGameError(error, redirectToLobby = false) {
    console.error('Game error:', error);
    
    if (error.includes("not found") || error.includes("not in this game") || redirectToLobby) {
        console.log('Game access error, returning to lobby');
        clearInterval(pollInterval);
        showScreen('lobby');
        startLobbyPolling();
        
        if (error.includes("not in this game")) {
            alert('Вы не являетесь участником этой игры. Возврат в лобби.');
        }
    }
}
    async function checkExistingGame() {
    try {
        // Просто пытаемся получить текущую сессию
        await getMySessionId();
        return false;
    } catch (error) {
        console.error('Error checking existing game:', error);
        return false;
    }
}
    // Обновление списка игроков в лобби
    function updatePlayersList(players) {
        console.log('Updating players list:', players);
        playersListElement.innerHTML = '';
        
        if (players && players.length > 0) {
            players.forEach(player => {
                const li = document.createElement('li');
                
                const colorSpan = document.createElement('span');
                colorSpan.className = 'player-color';
                colorSpan.style.backgroundColor = player.color;
                
                const nameSpan = document.createElement('span');
                nameSpan.textContent = player.name || `Игрок ${player.color}`;
                
                li.appendChild(colorSpan);
                li.appendChild(nameSpan);
                playersListElement.appendChild(li);
            });
        } else {
            playersListElement.innerHTML = '<li>Нет игроков в лобби</li>';
        }
    }
    // Функция проверки доступа к игре
async function checkGameAccess() {
    try {
        const response = await fetch('/Game/GameState');
        const gameData = await response.json();
        
        if (gameData.error) {
            console.log('No game access, returning to lobby');
            showScreen('lobby');
            startLobbyPolling();
            return false;
        }
        return true;
    } catch (error) {
        console.error('Game access check error:', error);
        return false;
    }
}

// В начале game polling
function startGamePolling() {
    if (pollInterval) clearInterval(pollInterval);
    
    console.log('Starting game polling for game:', currentGameId);
    gameIdDisplay.textContent = currentGameId;
    
    // Немедленно делаем первый запрос
    fetchGameState();
    
    // Затем запускаем интервал
    pollInterval = setInterval(fetchGameState, 1000);
}
    // Обновление списка игроков в игре
function updateGamePlayersList(players, currentPlayerIndex = 0) {
    console.log('Updating game players list:', players, 'Current index:', currentPlayerIndex);
    gamePlayersElement.innerHTML = '';
    
    if (players && players.length > 0) {
        players.forEach((player, index) => {
            const li = document.createElement('li');
            
            // Аватар игрока
            if (player.avatarPath) {
                const avatarImg = document.createElement('img');
                avatarImg.className = 'player-avatar';
                avatarImg.src = player.avatarPath;
                avatarImg.alt = player.name;
                li.appendChild(avatarImg);
            } else {
                const colorSpan = document.createElement('span');
                colorSpan.className = 'player-color';
                colorSpan.style.backgroundColor = player.color || '#FFFFFF';
                li.appendChild(colorSpan);
            }
            
            // Имя игрока
            const nameSpan = document.createElement('span');
            nameSpan.textContent = player.name || `Игрок ${player.color}`;
            
            // Индикатор текущего хода
            if (index === currentPlayerIndex) {
                nameSpan.innerHTML += ' 🎮';
            }
            
            li.appendChild(nameSpan);
            gamePlayersElement.appendChild(li);
        });
    } else {
        gamePlayersElement.innerHTML = '<li>Нет игроков</li>';
    }
}
    
    // Обновление игровой доски
function updateBoard(gameData) {
    if (!gameData || !gameData.cells) {
        console.log('No game data for board update');
        return;
    }
    
    console.log('Updating game board with dimensions:', gameData.width, 'x', gameData.height);
    boardElement.innerHTML = '';
    const width = gameData.width || 5;
    const height = gameData.height || 5;
    
    boardElement.style.gridTemplateColumns = `repeat(${width}, 60px)`;
    
    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const cellIndex = y * width + x;
            const cellData = gameData.cells[cellIndex];
            const cell = document.createElement('div');
            cell.className = 'cell' + (cellData?.blocked ? ' blocked' : '');
            cell.dataset.x = x;
            cell.dataset.y = y;
            
            // ДОБАВЛЯЕМ ОБРАБОТЧИК ДЛЯ ВСЕХ НЕЗАБЛОКИРОВАННЫХ ЯЧЕЕК
            if (cellData && !cellData.blocked) {
                cell.addEventListener('click', handleCellClick);
                cell.style.cursor = 'pointer';
            } else {
                cell.style.cursor = 'not-allowed';
            }
            
            if (cellData && cellData.owner) {
    // Ищем владельца ячейки среди игроков
    const ownerPlayer = gameData.players?.find(p => p.id === cellData.owner);
    if (ownerPlayer) {
        // ЗАПОЛНЯЕМ ВСЮ КЛЕТКУ ЦВЕТОМ ИГРОКА
        cell.style.backgroundColor = ownerPlayer.color;
        cell.style.backgroundSize = 'cover';
        cell.style.backgroundPosition = 'center';
        
        // ЕСЛИ ЕСТЬ АВАТАР - ИСПОЛЬЗУЕМ ЕГО КАК ФОН
        if (ownerPlayer.avatarPath) {
            cell.style.backgroundImage = `url(${ownerPlayer.avatarPath})`;
            cell.innerHTML = ''; // УБИРАЕМ ТЕКСТ
        } else {
            // ЕСЛИ АВАТАРА НЕТ - ПОКАЗЫВАЕМ ПЕРВУЮ БУКВУ ИМЕНИ
            cell.style.backgroundImage = 'none';
            cell.textContent = ownerPlayer.name ? ownerPlayer.name.charAt(0).toUpperCase() : '?';
            cell.style.color = 'white';
            cell.style.fontWeight = 'bold';
            cell.style.display = 'flex';
            cell.style.alignItems = 'center';
            cell.style.justifyContent = 'center';
            cell.style.fontSize = '20px';
        }
        
        // ДОБАВЛЯЕМ ИНДИКАТОР ПРОГРЕССА ДАЖЕ ДЛЯ ЗАНЯТЫХ КЛЕТОК
        const progressBar = document.createElement('div');
        progressBar.className = 'capture-progress';
        progressBar.style.width = '100%'; // ПОЛНАЯ ШИРИНА ДЛЯ ЗАНЯТЫХ КЛЕТОК
        progressBar.style.background = 'transparent'; // ПРОЗРАЧНЫЙ ДЛЯ ЗАНЯТЫХ КЛЕТОК
        progressBar.title = `Захвачено игроком: ${ownerPlayer.name}`;
        cell.appendChild(progressBar);
    }
} else {
    // ДЛЯ СВОБОДНЫХ КЛЕТОК - СТАНДАРТНАЯ ЛОГИКА ПРОГРЕССА
    const progressBar = document.createElement('div');
    progressBar.className = 'capture-progress';
    if (cellData && gameData.captureProgress > 0) {
        const progressPercent = (cellData.progress / gameData.captureProgress) * 100;
        progressBar.style.width = `${Math.min(progressPercent, 100)}%`;
        
        // ЦВЕТОВАЯ ИНДИКАЦИЯ ПРОГРЕССА
        if (progressPercent < 33) {
            progressBar.style.background = '#ff5252';
        } else if (progressPercent < 66) {
            progressBar.style.background = '#ffeb3b';
        } else {
            progressBar.style.background = '#4CAF50';
        }
    }
    cell.appendChild(progressBar);
}
            
            // Прогресс захвата ячейки
            const progressBar = document.createElement('div');
            progressBar.className = 'capture-progress';
            if (cellData && gameData.captureProgress) {
                const progressPercent = (cellData.progress / gameData.captureProgress) * 100;
                progressBar.style.width = `${Math.min(progressPercent, 100)}%`;
                
                // Визуальная индикация прогресса
                if (progressPercent < 33) {
                    progressBar.style.background = '#ff5252'; // Красный
                } else if (progressPercent < 66) {
                    progressBar.style.background = '#ffeb3b'; // Желтый
                } else {
                    progressBar.style.background = '#4CAF50'; // Зеленый
                }
            } else {
                progressBar.style.width = '0%';
            }
            cell.appendChild(progressBar);
            
            boardElement.appendChild(cell);
        }
    }
}
    
    // Обновление статуса игры
function updateStatus(gameData) {
    if (!gameData) return;
    
    const statusElement = document.getElementById('status');
    
    if (gameData.gameOver) {
        if (gameData.winner) {
            const winner = gameData.players?.find(p => p.id === gameData.winner);
            statusElement.textContent = winner ? `🎉 Победил ${winner.name}! 🎉` : '🎉 Игра окончена! 🎉';
        } else {
            statusElement.textContent = '🤝 Ничья!';
        }
        clearInterval(pollInterval);
    } else if (gameData.players && gameData.players.length > 0) {
        const currentPlayerIndex = gameData.currentPlayerIndex || 0;
        const currentPlayer = gameData.players[currentPlayerIndex];
        
        if (currentPlayer) {
            let statusText = `🎮 Ход: ${currentPlayer.name}`;
            
            if (currentPlayer.sessionId === mySessionId) {
                statusText += ' (Ваш ход! 👆)';
                statusElement.style.background = '#fff3cd';
                statusElement.style.color = '#856404';
            } else {
                statusElement.style.background = '#d1ecf1';
                statusElement.style.color = '#0c5460';
            }
            
            statusElement.textContent = statusText;
        }
    }
}
    
    // Обработчик клика по клетке
async function handleCellClick(e) {
    if (!currentGameId) {
        alert('Нет активной игры');
        return;
    }
    
    const x = parseInt(e.target.dataset.x);
    const y = parseInt(e.target.dataset.y);
    
    console.log('Cell clicked at:', x, y, 'Game ID:', currentGameId);
    
    try {
        const response = await fetch('/Game/MakeMove', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ X: x, Y: y })
        });
        
        const result = await response.json();
        console.log('Move response:', result);
        
        if (result.success) {
            fetchGameState();
        } else {
            alert(result.error || 'Неверный ход!');
        }
        
    } catch (error) {
        console.error('Ошибка выполнения хода:', error);
        alert('Ошибка соединения при выполнении хода');
    }
}
    // Проверка параметров URL
    function checkUrlParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const lobbyId = urlParams.get('lobbyId');
        
        if (lobbyId) {
            statusElement.textContent = 'Найдена ссылка на лобби. Выберите цвет и нажмите "Присоединиться"';
        }
    }
    
    // Инициализация
    checkUrlParams();
    showScreen('main');
    
    // Глобальная обработка ошибок
    window.addEventListener('error', function(e) {
        console.error('Global error:', e.error);
    });
    
    window.addEventListener('unhandledrejection', function(e) {
        console.error('Unhandled promise rejection:', e.reason);
    });
