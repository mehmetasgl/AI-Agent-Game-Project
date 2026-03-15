# AI-Agent-Game-Project
2D Procedural Dungeon &amp; AI Agent Project

EN

Overview

This project implements a 2D procedural dungeon generation system combined with an AI-controlled agent. Dungeons are generated using Binary Space Partitioning (BSP) for layout and Wave Function Collapse (WFC) for visual variety. The AI agent navigates these dungeons using A* pathfinding and a Finite State Machine (FSM) to make movement and combat decisions.


Features:

Procedural Dungeon Generation (PCG):

100×100 tile-based dungeons with multiple rooms and connecting corridors

Two dungeon themes: Medieval Dungeon & Dangerous Cave

Separate Tilemaps for floor, walls, objects, and decor


AI Agent:

Pathfinding with A* algorithm

Behavior controlled by FSM: Moving, Attacking, Evading, Goal-reaching

Decision-Making: AI evaluates enemies, distance, and health to choose actions

Dual combat modes: melee (<2 tiles) and ranged (2–8 tiles)


Enemies:

Stationary, patrolling, and chaser types

Spawn rules ensure fair gameplay and proper spacing

User Interface & Game Flow:

Demo UI Manager handles menu, gameplay interface, and level generation

Real-time display of player info: health, ammo, remaining enemies, distance to goal


Conclusion

The project demonstrates effective integration of procedural content generation and AI. BSP provides structured dungeons, WFC adds visual diversity, and FSM-based AI successfully navigates and engages enemies.


TR


Genel Bakış

Bu proje, AI kontrollü bir karakter ile birlikte 2D prosedürel zindan oluşturma sistemi sunar. Zindanlar, düzen için Binary Space Partitioning (BSP) ve görsellik için Wave Function Collapse (WFC) kullanılarak üretilir. AI ajan, bu zindanlarda A* pathfinding algoritması ve Finite State Machine (FSM) ile hareket ve savaş kararlarını alır.


Özellikler

Prosedürel Zindan Oluşturma (PCG):

100×100 tile tabanlı zindanlar, birden fazla oda ve bağlantı koridorları

İki zindan teması: Ortaçağ Zindanı & Tehlikeli Mağara

Zemin, duvar, nesne ve dekorlar ayrı Tilemap’lerde


AI Ajanı:

A* algoritması ile yol bulma

FSM ile hareket kontrolü: Hareket, Saldırı, Kaçma, Hedefe Ulaşma

Karar Alma Sistemi: AI düşmanları, mesafeyi ve sağlık durumunu değerlendirerek aksiyon seçer

İki savaş modu: yakın dövüş (<2 tile) ve menzilli (2–8 tile)


Düşmanlar:

Sabit, devriye ve kovalayan türler

Spawn kuralları adil ve dengeli oynanışı garanti eder

Kullanıcı Arayüzü ve Oyun Akışı:

Demo UI Manager, menü, oyun arayüzü ve seviye üretimini yönetir

Oyuncu bilgilerini gerçek zamanlı gösterir: sağlık, mermi, kalan düşman ve hedef uzaklığı


Sonuç

Proje, prosedürel içerik üretimi ve AI entegrasyonunu başarılı şekilde gösterir. BSP yapısal zindanlar sağlar, WFC görsel çeşitlilik sunar ve FSM tabanlı AI düşmanlarla başarıyla etkileşime girer.
