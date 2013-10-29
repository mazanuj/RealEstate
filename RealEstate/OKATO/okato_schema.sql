CREATE TABLE class_okato (
  id int(11) NOT NULL auto_increment COMMENT 'PK',
  `name` varchar(512) NOT NULL COMMENT 'Наименование',
  `code` varchar(11) NOT NULL COMMENT 'Код',
  control_number smallint(6) NOT NULL COMMENT 'Контрольное число',
  parent_id int(11) default NULL COMMENT 'Вышестоящий объект',
  parent_code varchar(11) default NULL COMMENT 'Код вышестоящего объекта',
  node_count smallint(6) NOT NULL default '0' COMMENT 'Количество вложенных в текущую ветку',
  additional_info varchar(128) default NULL COMMENT 'Дополнительные данные',
  PRIMARY KEY  (id),
  UNIQUE KEY `code` (`code`),
  KEY parent_id (parent_id),
  KEY parent_code (parent_code)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 COMMENT='ОК объектов административно-территориального деления ОКАТО';
ALTER TABLE `class_okato`
  ADD CONSTRAINT class_okato_ibfk_1 FOREIGN KEY (parent_id) REFERENCES class_okato (id);
