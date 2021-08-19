CREATE TABLE mailbox_mails (
   id int PRIMARY KEY NOT NULL AUTO_INCREMENT,
   sender_id varchar(50) references users(identifier),
   sender_firstname varchar(50),
   sender_lastname varchar(50),
   receiver_id varchar(50) references users(identifier),
   receiver_firstname varchar(50),
   receiver_lastname varchar(50),
   message text,
   opened boolean DEFAULT false
);