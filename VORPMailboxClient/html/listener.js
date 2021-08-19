let selectedLetter = null;

function showEditButtons(value) {
	document.getElementById("mailbox-buttons-letter").style.display = value === false ? 'none' : '';
}

function createNoMessageDiv() {
	if (document.getElementById('no-message') !== null) {
		return ;
	}

	const letterContainer = document.createElement('div');
	letterContainer.className = 'mailbox-letter-content';
	letterContainer.id = 'no-message';
	letterContainer.style.width = '700px';
	letterContainer.style.height = '100%';
	letterContainer.style.margin = '0 0 0 15px';
	letterContainer.style.color = 'white';
	letterContainer.style.display = 'flex';
	letterContainer.style.justifyContent = 'center';
	letterContainer.style.alignItems = 'center';

	const content = JSON.parse(window.localStorage.getItem('mailbox_language'))['UINoMessageReceived'];
	const noMessageText = document.createElement("p");
	noMessageText.textContent = content;
	noMessageText.style.fontSize = '30px';

	letterContainer.appendChild(noMessageText);

	document.getElementById("mailbox-content").appendChild(letterContainer);
}

function deleteNoMessageDiv() {
	const element = document.getElementById('no-message')
	if (element !== null) {
		element.parentElement.removeChild(element);
	}
}

function createLetterContent(data) {
	if (document.getElementById(`content-${data.id}`) !== null) {
		return ;
	}

	const letterContainer = document.createElement('div');
	letterContainer.className = 'mailbox-letter-content';
	letterContainer.id = `content-${data.id}`;
	letterContainer.style.width = '700px';
	letterContainer.style.height = '100%';
	letterContainer.style.margin = '0 0 0 15px';
	letterContainer.style.backgroundColor = 'black';
	letterContainer.style.opacity = '90%';
	letterContainer.style.color = 'white';
	letterContainer.style.border = '1px solid blue';
	letterContainer.style.display = 'none';

	const letterAuthor = document.createElement("div");
	letterAuthor.className = 'mailbox-letter-from';
	letterAuthor.id = `author-${data.id}`;
	letterAuthor.textContent = `${data.firstname} ${data.lastname}`;

	const letterContent = document.createElement("p");
	letterContent.className = 'mailbox-letter-message';
	letterContent.id = `message-${data.id}`;

	const parsedMessage = data.message.replaceAll('\n', "<br>");
	letterContent.innerHTML = parsedMessage;

	letterContainer.appendChild(letterContent);
	letterContainer.appendChild(letterAuthor);

	document.getElementById("mailbox-content").appendChild(letterContainer);
}

function createLetterTitle(data) {
	if (document.getElementById(`letter-${data.id}`) !== null) {
		return ;
	}

	const lettersContainer = document.getElementById("mailbox-letters");

	const newLetter = document.createElement("li");
	newLetter.className = 'mailbox-letter'
	newLetter.id = `letter-${data.id}`;


	const prefix = JSON.parse(window.localStorage.getItem('mailbox_language'))['UINamePrefix'];
	const letterTitle = document.createElement("a");
	letterTitle.text = `${prefix} ${data.firstname} ${data.lastname}`
	letterTitle.id = `title-${data.id}`;

	if (data.opened === false) {
		letterTitle.style.fontWeight = 'bold';
	}

	letterTitle.dataset.id = data.id;
	letterTitle.dataset.steam = data.steam;
	letterTitle.dataset.firstname = data.firstname;
	letterTitle.dataset.lastname = data.lastname;
	letterTitle.href = '#';

	letterTitle.onclick = (event) => {
		document.getElementById(event.target.id).style.fontWeight = '';
		const id = event.target.dataset.id;

		const letters = JSON.parse(window.localStorage.getItem('mailbox_letters')).map(letter => JSON.parse(letter));
		letters.forEach(letter => {
			if (letter.id === id) {
				console.log("set to true");
				letter.opened = true;
			}
		});
		window.localStorage.setItem('mailbox_letters', JSON.stringify(letters));

		showEditButtons(true);
		selectedLetter = event.target.dataset;

		const lettersElements = document.getElementsByClassName('mailbox-letter-content');
		for (let i = lettersElements.length - 1; i >= 0; i--) {
			if (lettersElements.item(i).id !== `content-${id}`) {
				lettersElements.item(i).style.display = 'none';
			} else {
				lettersElements.item(i).style.display = '';
			}
		}
	}

	newLetter.appendChild(letterTitle);

	lettersContainer.appendChild(newLetter);
}

function createLetter(data) {
	createLetterTitle(data);
	createLetterContent(data);
}

function createUserSelectOption(name, id) {
	if (document.getElementById(name) !== null) {
		return ;
	}
	const userSelect = document.getElementById("mailbox-user-select");

	const userOption = document.createElement('option');
	userOption.textContent = name;
	userOption.id = name;
	userOption.value = id;

	userSelect.appendChild(userOption);
}

function navigateToWriteSection() {
	document.getElementById('mailbox-container-write').hidden = false;
	document.getElementById('mailbox-container-read').hidden = true;
}

function navigateToReadSection() {
	document.getElementById('mailbox-container-write').hidden = true;
	document.getElementById('mailbox-container-read').hidden = false;
}

function closeAllLetters() {
	const lettersElements = document.getElementsByClassName('mailbox-letter-content');
	for (let i = lettersElements.length - 1; i >= 0; i--) {
		lettersElements.item(i).style.display = 'none';
	}
}

function setMessages(messages) {
	console.log(messages);

	if (messages.length === 0) {
		createNoMessageDiv();
		return ;
	} else {
		deleteNoMessageDiv();
	}

	// load letters into list
	window.localStorage.setItem('mailbox_letters', JSON.stringify(messages));
	messages.forEach((letter) => {
		const parsedLetter = JSON.parse(letter);
		createLetter(parsedLetter);
	});
}

function setUsers(users) {
	//load users into select
	window.localStorage.setItem('mailbox_users', JSON.stringify(users));
	createUserSelectOption('Choisis un destinataire', 0);
	users.forEach((user, index) => {
		const parsedUser = JSON.parse(user);
		createUserSelectOption(`${parsedUser.firstname} ${parsedUser.lastname}`, index + 1);
	});
}

function setLanguage(language) {
	window.localStorage.setItem('mailbox_language', JSON.stringify(language));

	document.getElementById('mailbox-read-button-close').textContent = language['UICloseButton'];
	document.getElementById('mailbox-button-write').textContent = language['UIWriteButton'];
	document.getElementById('mailbox-button-delete').textContent = language['UIDeleteButton'];
	document.getElementById('mailbox-button-answer').textContent = language['UIAnswerButton'];

	document.getElementById('mailbox-write-button-close').textContent = language['UICloseButton'];
	document.getElementById('mailbox-button-cancel').textContent = language['UIAbortButton'];
	document.getElementById('mailbox-button-send').textContent = language['UISendButton'];
}


window.onload = () => {
	//init windows
	navigateToReadSection();
	showEditButtons(false);

}

window.addEventListener('message', (event) => {
	/**
	 * @type {{
	 *     action: string,
	 *     users: string,
	 *     messages: string,
	 *     language: string
	 * }}
	 * */
	const message = event.data;

	console.log(message);

	switch (message.action) {
		case 'open':
			$("body").show();
			break;
		case 'close':
			$("body").hide();
			break;
		case 'set_messages':
			setMessages(JSON.parse(message.messages));
			break;
		case 'set_users':
			setUsers(JSON.parse(message.users));
			break;
		case 'set_language':
			setLanguage(JSON
				.parse(message.language))
			break;
		default:
			return;
	}

	// add UI buttons interaction
	$('#mailbox-button-cancel').unbind().click(() => {
		navigateToReadSection();
	});

	$('#mailbox-button-write').unbind().click(() => {
		document.getElementById("mailbox-user-select").selectedIndex = '0';
		navigateToWriteSection();
	});

	$('#mailbox-button-answer').unbind().click(() => {
		const index = JSON.parse(window.localStorage.getItem('mailbox_users')).findIndex(user => {
			return user.steam === selectedLetter.steam && user.firstname === selectedLetter.firstname && user.lastname === selectedLetter.lastname;
		});

		if (index < 0) {
			return ;
		}

		document.getElementById("mailbox-user-select").selectedIndex = (index + 1).toString();
		navigateToWriteSection();
	});

	$('#mailbox-button-delete').unbind().click(() => {
		const titleElement = document.getElementById(`letter-${selectedLetter.id}`);
		const contentElement = document.getElementById(`content-${selectedLetter.id}`);

		titleElement.parentElement.removeChild(titleElement);
		contentElement.parentElement.removeChild(contentElement);
		showEditButtons(false);

		const letters = JSON.parse(window.localStorage.getItem('mailbox_letters'));
		window.localStorage.setItem('mailbox_letters', JSON.stringify(letters.filter(letter => letter.id !== selectedLetter.id)));
	});

	$('#mailbox-button-send').unbind().click(() => {
		const message = document.getElementById('mailbox-message-text').value;

		const author = JSON.parse(window.localStorage.getItem('mailbox_users'))[document.getElementById('mailbox-user-select').selectedIndex - 1];

		if (!!author && message.length > 0) {
			// send to Client
			$.post('http://vorp_mailbox/send', JSON.stringify({author, message}));
			document.getElementById('mailbox-message-text').value = '';
			navigateToReadSection();
		}
	});

	$('#mailbox-write-button-close').unbind().click(() => {
		closeAllLetters();
		const messages = window.localStorage.getItem('mailbox_letters');
		console.log(messages);
		$.post('http://vorp_mailbox/close', JSON.stringify({messages}));
	});
	$('#mailbox-read-button-close').unbind().click(() => {
		closeAllLetters();
		const messages = window.localStorage.getItem('mailbox_letters');
		$.post('http://vorp_mailbox/close', JSON.stringify({messages}));
	});
});
