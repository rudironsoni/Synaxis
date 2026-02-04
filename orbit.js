/**
 * Orbit - Alien Signal Decryption Game
 * A color sequence prediction game where players attempt to decrypt
 * an alien signal by guessing the correct color pattern.
 */

class OrbitGame {
    constructor() {
        this.colors = ['red', 'blue', 'green', 'yellow', 'purple', 'cyan'];
        this.codeLength = 4;
        this.maxAttempts = 8;
        this.secretCode = [];
        this.currentAttempt = [];
        this.attempts = [];
        this.currentSlotIndex = 0;
        this.gameOver = false;
        
        this.init();
    }

    init() {
        this.cacheElements();
        this.bindEvents();
        this.generateSecretCode();
        this.renderGameBoard();
        this.updateFeedback('Select colors to begin decryption...', 'info');
    }

    cacheElements() {
        this.gameBoard = document.getElementById('gameBoard');
        this.predictionSlots = document.querySelectorAll('#predictionSlots .slot');
        this.submitBtn = document.getElementById('submitBtn');
        this.attemptsLeftEl = document.getElementById('attemptsLeft');
        this.feedbackEl = document.getElementById('feedback');
        this.secretCodeEl = document.getElementById('secretCode');
        this.codeSlotsEl = document.getElementById('codeSlots');
        this.gameOverlay = document.getElementById('gameOverlay');
        this.gameResultEl = document.getElementById('gameResult');
        this.gameMessageEl = document.getElementById('gameMessage');
        this.restartBtn = document.getElementById('restartBtn');
        this.colorBtns = document.querySelectorAll('.color-btn');
    }

    bindEvents() {
        // Color palette clicks
        this.colorBtns.forEach(btn => {
            btn.addEventListener('click', (e) => this.handleColorSelect(e));
        });

        // Prediction slot clicks (to clear)
        this.predictionSlots.forEach((slot, index) => {
            slot.addEventListener('click', () => this.handleSlotClick(index));
        });

        // Submit button
        this.submitBtn.addEventListener('click', () => this.submitAttempt());

        // Restart button
        this.restartBtn.addEventListener('click', () => this.restart());

        // Keyboard support
        document.addEventListener('keydown', (e) => this.handleKeydown(e));
    }

    generateSecretCode() {
        this.secretCode = [];
        for (let i = 0; i < this.codeLength; i++) {
            const randomColor = this.colors[Math.floor(Math.random() * this.colors.length)];
            this.secretCode.push(randomColor);
        }
        console.log('Debug - Secret Code:', this.secretCode); // For testing
    }

    handleColorSelect(e) {
        if (this.gameOver) return;
        
        const color = e.target.dataset.color;
        
        if (this.currentSlotIndex < this.codeLength) {
            this.currentAttempt[this.currentSlotIndex] = color;
            this.updatePredictionDisplay();
            this.currentSlotIndex++;
            this.updateSubmitButton();
        }
    }

    handleSlotClick(index) {
        if (this.gameOver) return;
        
        // Remove the color at this index and shift remaining
        this.currentAttempt.splice(index, 1);
        this.currentSlotIndex = this.currentAttempt.length;
        this.updatePredictionDisplay();
        this.updateSubmitButton();
    }

    handleKeydown(e) {
        if (this.gameOver) return;
        
        // Number keys 1-6 for colors
        const keyMap = {
            '1': 'red', '2': 'blue', '3': 'green',
            '4': 'yellow', '5': 'purple', '6': 'cyan'
        };
        
        if (keyMap[e.key] && this.currentSlotIndex < this.codeLength) {
            this.currentAttempt[this.currentSlotIndex] = keyMap[e.key];
            this.updatePredictionDisplay();
            this.currentSlotIndex++;
            this.updateSubmitButton();
        }
        
        // Backspace to remove last color
        if (e.key === 'Backspace' && this.currentSlotIndex > 0) {
            this.currentAttempt.pop();
            this.currentSlotIndex--;
            this.updatePredictionDisplay();
            this.updateSubmitButton();
        }
        
        // Enter to submit
        if (e.key === 'Enter' && this.currentAttempt.length === this.codeLength) {
            this.submitAttempt();
        }
    }

    updatePredictionDisplay() {
        this.predictionSlots.forEach((slot, index) => {
            slot.className = 'slot';
            if (this.currentAttempt[index]) {
                slot.classList.add(this.currentAttempt[index], 'filled');
            }
            if (index === this.currentSlotIndex && !this.gameOver) {
                slot.classList.add('active');
            }
        });
    }

    updateSubmitButton() {
        this.submitBtn.disabled = this.currentAttempt.length !== this.codeLength;
    }

    submitAttempt() {
        if (this.currentAttempt.length !== this.codeLength || this.gameOver) return;

        const feedback = this.evaluateAttempt(this.currentAttempt);
        this.attempts.push({
            colors: [...this.currentAttempt],
            feedback: feedback
        });

        this.renderGameBoard();
        
        // Check win condition
        if (feedback.correct === this.codeLength) {
            this.handleWin();
        } else if (this.attempts.length >= this.maxAttempts) {
            this.handleLoss();
        } else {
            // Reset for next attempt
            this.currentAttempt = [];
            this.currentSlotIndex = 0;
            this.updatePredictionDisplay();
            this.updateSubmitButton();
            this.updateAttemptsLeft();
            
            const messages = [
                'Signal interference detected. Try again.',
                'Pattern not recognized. Adjust sequence.',
                'Decryption failed. Analyze feedback.',
                'Signal remains encrypted. Continue attempt.'
            ];
            const randomMessage = messages[Math.floor(Math.random() * messages.length)];
            this.updateFeedback(randomMessage, 'info');
        }
    }

    evaluateAttempt(attempt) {
        let correct = 0;
        let partial = 0;
        
        const secretCopy = [...this.secretCode];
        const attemptCopy = [...attempt];
        
        // Check for correct position
        for (let i = 0; i < this.codeLength; i++) {
            if (attemptCopy[i] === secretCopy[i]) {
                correct++;
                secretCopy[i] = null;
                attemptCopy[i] = null;
            }
        }
        
        // Check for correct color, wrong position
        for (let i = 0; i < this.codeLength; i++) {
            if (attemptCopy[i] !== null) {
                const indexInSecret = secretCopy.indexOf(attemptCopy[i]);
                if (indexInSecret !== -1) {
                    partial++;
                    secretCopy[indexInSecret] = null;
                }
            }
        }
        
        return { correct, partial };
    }

    renderGameBoard() {
        this.gameBoard.innerHTML = '';
        
        this.attempts.forEach((attempt, index) => {
            const row = document.createElement('div');
            row.className = 'attempt-row';
            
            // Attempt number
            const attemptNum = document.createElement('span');
            attemptNum.className = 'attempt-number';
            attemptNum.textContent = `${index + 1}.`;
            row.appendChild(attemptNum);
            
            // Color slots
            const colorsDiv = document.createElement('div');
            colorsDiv.className = 'attempt-colors';
            attempt.colors.forEach(color => {
                const slot = document.createElement('div');
                slot.className = `slot filled ${color}`;
                colorsDiv.appendChild(slot);
            });
            row.appendChild(colorsDiv);
            
            // Feedback pegs
            const pegsDiv = document.createElement('div');
            pegsDiv.className = 'attempt-pegs';
            
            // Add correct pegs
            for (let i = 0; i < attempt.feedback.correct; i++) {
                const peg = document.createElement('span');
                peg.className = 'peg correct';
                pegsDiv.appendChild(peg);
            }
            
            // Add partial pegs
            for (let i = 0; i < attempt.feedback.partial; i++) {
                const peg = document.createElement('span');
                peg.className = 'peg partial';
                pegsDiv.appendChild(peg);
            }
            
            // Fill remaining with empty
            const totalPegs = attempt.feedback.correct + attempt.feedback.partial;
            for (let i = totalPegs; i < this.codeLength; i++) {
                const peg = document.createElement('span');
                peg.className = 'peg';
                pegsDiv.appendChild(peg);
            }
            
            row.appendChild(pegsDiv);
            this.gameBoard.appendChild(row);
        });
        
        // Scroll to bottom
        this.gameBoard.scrollTop = this.gameBoard.scrollHeight;
    }

    updateAttemptsLeft() {
        const remaining = this.maxAttempts - this.attempts.length;
        this.attemptsLeftEl.textContent = remaining;
        
        if (remaining <= 2) {
            this.attemptsLeftEl.style.color = 'var(--color-danger)';
        } else if (remaining <= 4) {
            this.attemptsLeftEl.style.color = 'var(--color-warning)';
        }
    }

    updateFeedback(message, type) {
        this.feedbackEl.textContent = message;
        this.feedbackEl.className = `feedback ${type}`;
    }

    handleWin() {
        this.gameOver = true;
        this.updateFeedback('Signal decrypted successfully!', 'success');
        this.showGameOver(true);
    }

    handleLoss() {
        this.gameOver = true;
        this.revealSecretCode();
        this.updateFeedback('Signal lost. Decryption failed.', 'error');
        this.showGameOver(false);
    }

    revealSecretCode() {
        this.codeSlotsEl.innerHTML = '';
        this.secretCode.forEach(color => {
            const slot = document.createElement('div');
            slot.className = `slot filled ${color}`;
            this.codeSlotsEl.appendChild(slot);
        });
        this.secretCodeEl.classList.remove('hidden');
    }

    showGameOver(won) {
        setTimeout(() => {
            if (won) {
                this.gameResultEl.textContent = 'SIGNAL DECRYPTED';
                this.gameResultEl.className = 'victory';
                this.gameMessageEl.textContent = `You decrypted the alien signal in ${this.attempts.length} attempt${this.attempts.length !== 1 ? 's' : ''}!`;
            } else {
                this.gameResultEl.textContent = 'SIGNAL LOST';
                this.gameResultEl.className = 'defeat';
                this.gameMessageEl.textContent = 'The alien signal remains encrypted...';
            }
            this.gameOverlay.classList.remove('hidden');
        }, 500);
    }

    restart() {
        // Reset game state
        this.secretCode = [];
        this.currentAttempt = [];
        this.attempts = [];
        this.currentSlotIndex = 0;
        this.gameOver = false;
        
        // Reset UI
        this.gameBoard.innerHTML = '';
        this.secretCodeEl.classList.add('hidden');
        this.gameOverlay.classList.add('hidden');
        this.attemptsLeftEl.textContent = this.maxAttempts;
        this.attemptsLeftEl.style.color = 'var(--color-accent)';
        
        // Reset prediction slots
        this.predictionSlots.forEach(slot => {
            slot.className = 'slot';
        });
        
        // Generate new code
        this.generateSecretCode();
        this.updatePredictionDisplay();
        this.updateSubmitButton();
        this.updateFeedback('Select colors to begin decryption...', 'info');
    }
}

// Initialize game when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new OrbitGame();
});
