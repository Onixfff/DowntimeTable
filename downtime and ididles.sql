-- MySQL dump 10.13  Distrib 8.0.23, for Win64 (x86_64)
--
-- Host: localhost    Database: spslogger
-- ------------------------------------------------------
-- Server version	8.0.23

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `downtime`
--

DROP TABLE IF EXISTS `downtime`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `downtime` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Timestamp` datetime NOT NULL,
  `Difference` time NOT NULL,
  `idIdle` int NOT NULL,
  `Comment` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `downtime`
--

LOCK TABLES `downtime` WRITE;
/*!40000 ALTER TABLE `downtime` DISABLE KEYS */;
INSERT INTO `downtime` VALUES (5,'2024-05-15 10:32:00','00:31:30',2,'Привет мир'),(6,'2024-05-15 11:11:00','03:06:30',6,'Пока'),(7,'2024-05-15 14:25:00','02:25:41',12,'Ой'),(8,'2024-05-19 10:32:00','00:31:30',2,'Eeee ууууу'),(9,'2024-05-19 11:11:00','03:06:30',2,'укцкуц цц'),(10,'2024-05-19 14:25:00','02:25:41',2,'Eeeeец цу уку'),(11,'2024-05-19 10:32:00','00:31:30',2,'Eeee нннн'),(12,'2024-05-19 11:11:00','03:06:30',2,'Eeee кууку'),(13,'2024-05-19 14:25:00','02:25:41',5,'Eeee ыв'),(14,'2024-06-04 15:32:00','00:30:50',3,'ttrere'),(15,'2024-06-04 16:10:20','01:16:10',4,'yyree'),(16,'2024-06-04 10:32:00','00:31:30',12,'Привет мир'),(20,'2024-06-04 11:11:00','03:06:30',6,'Привет мир'),(21,'2024-06-04 14:25:00','02:25:41',6,'Пока'),(22,'2024-06-04 16:58:11','00:49:30',5,'ну ппп');
/*!40000 ALTER TABLE `downtime` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ididles`
--

DROP TABLE IF EXISTS `ididles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ididles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ididles`
--

LOCK TABLES `ididles` WRITE;
/*!40000 ALTER TABLE `ididles` DISABLE KEYS */;
INSERT INTO `ididles` VALUES (1,'Плановая остановка'),(2,'Отказ оборудования'),(3,'Отсутствие сырья'),(4,'Экспериментальные работы'),(5,'Медленное твердение массивов'),(6,'Прочее'),(7,'Смена типа резки/настройка РК'),(8,'Организация производства'),(9,'смена/отработка рецептуры'),(10,'Выходной день'),(11,'Отсутствие шлама'),(12,'Разгрузка вагонов');
/*!40000 ALTER TABLE `ididles` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2024-06-05 10:47:01
