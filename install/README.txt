Для установки требуются:
	* .net 4
	* mysql 5.5
	
Перед загрузкой приложения:
	1. запустить коммандную строку mysql
	
	2. increase mysql max_allowed_packet for client:
	//////////////////////////////////////////////////////
		C:\>mysql -u root -p
		mysql> show variables like 'max_allowed_packet';
		+--------------------+----------+
		| Variable_name      | Value    |
		+--------------------+----------+
		| max_allowed_packet | 16777216 |
		+--------------------+----------+

		mysql> set max_allowed_packet=1024 * 1024 * 512;
		ERROR 1621 (HY000): SESSION variable 'max_allowed_packet' is read-only. Use SET GLOBAL to assign the value

		mysql> set global max_allowed_packet=1024 * 1024 * 512;
		Query OK, 0 rows affected (0.00 sec)

		mysql> show variables like 'max_allowed_packet';
		+--------------------+----------+
		| Variable_name      | Value    |
		+--------------------+----------+
		| max_allowed_packet | 16777216 |
		+--------------------+----------+

		mysql> exit

		C:\>mysql -u root -p

		mysql> show variables like 'max_allowed_packet';
		+--------------------+-----------+
		| Variable_name      | Value     |
		+--------------------+-----------+
		| max_allowed_packet | 536870912 |
		+--------------------+-----------+
	/////////////////////////////////////////
	
	3. create anonymous user
	/////////////////////////////////////////
		mysql> CREATE USER ''@'localhost';
	/////////////////////////////////////////
	
	4. create database (имя указано в RealEstate.exe.config)
	//////////////////////////////////////////////////////////////
		mysql> CREATE DATABASE RealEstate;
	//////////////////////////////////////////////////////////////
	
	5. настроить строку подключения к базе данных в RealEstate.exe.config
		(connectionString="Server=.\SQLExpress;Database=RealEstate;Integrated Security=True;MultipleActiveResultSets=True")
	
