document.addEventListener('DOMContentLoaded', function() {
    // Элементы DOM
    const nInput = document.getElementById('n');
    const bInput = document.getElementById('b');
    const dInput = document.getElementById('d');
    const tInput = document.getElementById('t');
    const uInput = document.getElementById('u');
    const playerNameInput = document.getElementById('playerName');
    const startButton = document.getElementById('start');
    const restartButton = document.getElementById('restart');
    const boardElement = document.getElementById('board');
    const statusElement = document.getElementById('status');
    const tokenElements = document.querySelectorAll('.token');
    
    let selectedColor = null;
    let gameId = null;
    let currentPlayerColor = null;
    let myColor = null;
    let gameInterval = null;
    let gameState = null;
    
    // Обработчики выбора токена
    tokenElements.forEach(token => {
        token.addEventListener('click', () => {
            tokenElements.forEach(t => t.classList.remove('selected'));
            token.classList.add('selected');
            selectedColor = token.getAttribute('data-color');
        });
    });
    
    // Обработчик начала игры
    startButton.addEventListener('click', async () => {
        if (!selectedColor || !playerNameInput.value) {
            alert('Пожалуйста, выберите цвет и введите имя!');
            return;
        }
        
        const params = {
            Width: parseInt(nInput.value),
            Height: parseInt(bInput.value),
            BlockChance: parseInt(dInput.value),
            WinLength: parseInt(tInput.value),
            CaptureProgress: parseInt(uInput.value)
        };
        
        try {
            // Создаем игру
            const createResponse = await fetch('/Game/CreateGame', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(params)
            });
            
            const createResult = await createResponse.json();
            
            if (!createResult.success) {
                alert('Ошибка создания игры: ' + createResult.error);
                return;
            }
            
            gameId = createResult.gameId;
            
            // Присоединяемся к игре
            const joinResponse = await fetch('/Game/JoinGame', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    GameId: gameId,
                    Color: selectedColor,
                    Name: playerNameInput.value
                })
            });
            
            const joinResult = await joinResponse.json();
            
            if (joinResult.success) {
                myColor = selectedColor;
                startButton.disabled = true;
                restartButton.disabled = false;
                
                // Начинаем опрос состояния игры
                startGamePolling();
            } else {
                alert('Не удалось присоединиться к игре. Возможно, этот цвет уже занят.');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Произошла ошибка при создании игры');
        }
    });
    
    // Обработчик рестарта
    restartButton.addEventListener('click', () => {
        if (gameInterval) clearInterval(gameInterval);
        location.reload();
    });
    
    // Функция опроса состояния игры
    function startGamePolling() {
        if (gameInterval) clearInterval(gameInterval);
        
        gameInterval = setInterval(async () => {
            try {
                const response = await fetch('/Game/GameState');
                const gameData = await response.json();
                
                if (gameData.error) {
                    console.error(gameData.error);
                    return;
                }
                
                gameState = gameData;
                
                // Обновляем доску
                updateBoard(gameData);
                
                // Обновляем статус
                updateStatus(gameData);
                
            } catch (error) {
                console.error('Ошибка при получении состояния игры:', error);
            }
        }, 2000);
    }
    
    // Функция обновления игровой доски
    function updateBoard(gameData) {
        boardElement.innerHTML = '';
        boardElement.style.width = `${gameData.width * 44}px`;
        
        for (let y = 0; y < gameData.height; y++) {
            for (let x = 0; x < gameData.width; x++) {
                const cellIndex = y * gameData.width + x;
                const cellData = gameData.cells[cellIndex];
                const cell = document.createElement('div');
                cell.className = 'cell' + (cellData.blocked ? ' blocked' : '');
                cell.dataset.x = cellData.x;
                cell.dataset.y = cellData.y;
                
                if (!cellData.blocked && cellData.owner === null) {
                    cell.addEventListener('click', handleCellClick);
                }
                
                if (cellData.owner) {
                    const owner = gameData.players.find(p => p.id === cellData.owner);
                    if (owner) {
                        cell.style.backgroundColor = owner.color;
                    }
                }
                
                const progressBar = document.createElement('div');
                progressBar.className = 'capture-progress';
                progressBar.style.width = `${(cellData.progress / gameData.captureProgress) * 100}%`;
                cell.appendChild(progressBar);
                
                boardElement.appendChild(cell);
            }
            boardElement.appendChild(document.createElement('br'));
        }
    }
    
    // Функция обновления статуса
    function updateStatus(gameData) {
        if (gameData.gameOver) {
            if (gameData.winner) {
                const winner = gameData.players.find(p => p.id === gameData.winner);
                statusElement.textContent = `Игра окончена! Победил ${winner.name} (${winner.color})!`;
            } else {
                statusElement.textContent = 'Игра окончена! Ничья!';
            }
            clearInterval(gameInterval);
        } else {
            currentPlayerColor = gameData.currentPlayer;
            if (gameData.players && gameData.players.length > 0) {
                const currentPlayer = gameData.players[gameData.currentPlayerIndex];
                statusElement.textContent = `Ход игрока ${currentPlayer.name} (${currentPlayer.color})`;
                
                // Если сейчас наш ход
                if (currentPlayerColor === myColor) {
                    statusElement.textContent += ' (Ваш ход!)';
                }
            }
        }
    }
    
    // Обработчик клика по клетке
    async function handleCellClick(e) {
        if (!gameState || gameState.gameOver) return;
        if (currentPlayerColor !== myColor) {
            alert('Сейчас не ваш ход!');
            return;
        }
        
        const x = parseInt(e.target.dataset.x);
        const y = parseInt(e.target.dataset.y);
        
        try {
            const response = await fetch('/Game/MakeMove', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ X: x, Y: y })
            });
            
            const result = await response.json();
            
            if (result.success) {
                // Немедленно обновляем состояние
                const gameResponse = await fetch('/Game/GameState');
                const newGameState = await gameResponse.json();
                gameState = newGameState;
                
                updateBoard(newGameState);
                updateStatus(newGameState);
            } else {
                alert('Неверный ход! ' + (result.error || ''));
            }
        } catch (error) {
            console.error('Ошибка при выполнении хода:', error);
        }
    }
});