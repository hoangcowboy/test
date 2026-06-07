const boardElement = document.getElementById('board');
const resetButton = document.getElementById('resetButton');
const chooseBlackButton = document.getElementById('chooseBlack');
const chooseWhiteButton = document.getElementById('chooseWhite');
const currentPlayerElement = document.getElementById('current-player');
const messageElement = document.getElementById('message');
const BOARD_SIZE = 15;
let board = [];
let currentPlayer = null;
let gameOver = false;
let colorSelectionLocked = false;
function createEmptyBoard(){const r=[];for(let i=0;i<BOARD_SIZE;i++){const row=[];for(let j=0;j<BOARD_SIZE;j++){row.push(0)}r.push(row)}return r}
function isDarkSquare(row,col){return (row+col)%2===1}
function setMessage(t){messageElement.textContent=t}
function updateCurrentPlayerText(){currentPlayerElement.textContent=currentPlayer==='black'?'Đen':currentPlayer==='white'?'Trắng':'Chưa chọn'}
function renderBoard(){boardElement.innerHTML='';for(let r=0;r<BOARD_SIZE;r++){for(let c=0;c<BOARD_SIZE;c++){const s=document.createElement('div');s.className=`square ${isDarkSquare(r,c)?'dark':'light'}`;s.dataset.row=r;s.dataset.col=c;const p=board[r][c];if(p!==0){const el=document.createElement('div');el.className=`piece ${p===1?'white':'black'}`;s.appendChild(el)}s.addEventListener('click',onSquareClick);boardElement.appendChild(s)}}updateCurrentPlayerText()}
function checkWin(row,col,val){const dirs=[{dr:0,dc:1},{dr:1,dc:0},{dr:1,dc:1},{dr:1,dc:-1}];for(const {dr,dc} of dirs){let cnt=1;for(let step=1;step<5;step++){const r=row+dr*step,c=col+dc*step;if(r<0||r>=BOARD_SIZE||c<0||c>=BOARD_SIZE)break;if(board[r][c]===val)cnt++;else break}for(let step=1;step<5;step++){const r=row-dr*step,c=col-dc*step;if(r<0||r>=BOARD_SIZE||c<0||c>=BOARD_SIZE)break;if(board[r][c]===val)cnt++;else break}if(cnt>=5)return true}return false}
function triggerFireworks(winner){const fireworks=document.getElementById('fireworks');const colors=winner==='white'?['#fff7a8','#ffd1ff','#aee7ff','#ffdbda','#f5d6ff']:['#ffd700','#ff6b6b','#6bc1ff','#ffa500','#fff'];for(let i=0;i<36;i++){const f=document.createElement('div');f.className='firework';const size=10+Math.random()*18;const x=Math.random()*100;const y=Math.random()*24+8;const angle=Math.random()*Math.PI*2;const dist=140+Math.random()*180;const tx=Math.cos(angle)*dist;const ty=Math.sin(angle)*dist;const color=colors[Math.floor(Math.random()*colors.length)];const delay=Math.random()*0.3;f.style.left=`${x}vw`;f.style.top=`${y}vh`;f.style.width=`${size}px`;f.style.height=`${size}px`;f.style.background=color;f.style.filter=`drop-shadow(0 0 18px ${color})`;f.style.animationDelay=`${delay}s`;f.style.setProperty('--tx',`${tx}px`);f.style.setProperty('--ty',`${ty}px`);fireworks.appendChild(f);setTimeout(()=>f.remove(),2000+delay*1000)}}
function setColorSelectionEnabled(enabled){
  colorSelectionLocked = !enabled;
  chooseBlackButton.disabled = !enabled;
  chooseWhiteButton.disabled = !enabled;
}

function onSquareClick(e){
  if(gameOver){setMessage('Ván đã kết thúc. Nhấn Làm mới ván mới để chơi lại.');return}
  if(!currentPlayer){setMessage('Chọn quân trước khi đặt.');return}
  const r=Number(e.currentTarget.dataset.row),c=Number(e.currentTarget.dataset.col);
  if(board[r][c]!==0){setMessage('Ô này đã có quân.');return}
  board[r][c]=currentPlayer==='black'?2:1;
  if(!colorSelectionLocked){
    setColorSelectionEnabled(false);
  }
  renderBoard();
  const won=checkWin(r,c,board[r][c]);
  if(won){setMessage(`Quân ${currentPlayer==='black'?'đen':'trắng'} thắng!`);triggerFireworks(currentPlayer);gameOver=true;return}
  currentPlayer=currentPlayer==='black'?'white':'black';
  updateCurrentPlayerText();
  setMessage(`Đã đặt quân. Lượt ${currentPlayer==='black'?'đen':'trắng'}.`)
}
function resetGame(){
  board=createEmptyBoard();
  currentPlayer=null;
  gameOver=false;
  setColorSelectionEnabled(true);
  chooseBlackButton.classList.remove('active');
  chooseWhiteButton.classList.remove('active');
  updateCurrentPlayerText();
  setMessage('Bấm chọn màu trắng hoặc đen để bắt đầu.');
  renderBoard();
}
chooseBlackButton.addEventListener('click', () => {
	if (gameOver) resetGame();
	currentPlayer = 'black';
	updateCurrentPlayerText();
	setMessage('Bạn đã chọn quân đen. Chọn một ô trống để đặt quân.');
	chooseBlackButton.classList.add('active');
	chooseWhiteButton.classList.remove('active');
});

chooseWhiteButton.addEventListener('click', () => {
	if (gameOver) resetGame();
	currentPlayer = 'white';
	updateCurrentPlayerText();
	setMessage('Bạn đã chọn quân trắng. Chọn một ô trống để đặt quân.');
	chooseWhiteButton.classList.add('active');
	chooseBlackButton.classList.remove('active');
});

resetButton.addEventListener('click', () => {
  resetGame();
});

resetGame();
