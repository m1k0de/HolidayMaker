-- =========================================
-- HolidayMaker – databas för bokning av boenden
-- Kör hela skriptet för att skapa databasen från början med testdata.
-- =========================================
DROP DATABASE IF EXISTS holidaymaker;
CREATE DATABASE holidaymaker CHARACTER SET utf8mb4 COLLATE utf8mb4_swedish_ci;
USE holidaymaker;

-- ORTER: egen tabell så att samma ort inte upprepas (normalisering)
CREATE TABLE location (
    id   INT AUTO_INCREMENT PRIMARY KEY,
    city VARCHAR(100) NOT NULL,
    CONSTRAINT uq_location_city UNIQUE (city)
);

-- BOENDEN: city_id är främmande nyckel till location. Avstånd i meter, omdöme 1–5.
CREATE TABLE accommodations (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(100) NOT NULL,
    city_id        INT NOT NULL,
    pool           BOOLEAN NOT NULL,
    restaurant     BOOLEAN NOT NULL,
    beach_distance INT NOT NULL,
    city_distance  INT NOT NULL,
    rating         INT NOT NULL,
    description    TEXT,
    CONSTRAINT fk_accommodations_location FOREIGN KEY (city_id) REFERENCES location(id),
    CONSTRAINT chk_rating         CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT chk_beach_distance CHECK (beach_distance >= 0),
    CONSTRAINT chk_city_distance  CHECK (city_distance >= 0)
);

-- RUM: hör till ett boende. room_number är text, t.ex. 'Hytt 1',
-- och är unikt inom sitt boende (två boenden får ha samma rumsnamn).
CREATE TABLE rooms (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    accommodation_id INT NOT NULL,
    room_number      VARCHAR(50) NOT NULL,
    price_per_night  DECIMAL(10,2) NOT NULL,
    capacity         INT NOT NULL,
    CONSTRAINT fk_rooms_accommodations FOREIGN KEY (accommodation_id) REFERENCES accommodations(id),
    CONSTRAINT uq_rooms_accommodation_room UNIQUE (accommodation_id, room_number),
    CONSTRAINT chk_capacity CHECK (capacity >= 1),
    CONSTRAINT chk_price    CHECK (price_per_night > 0)
);

-- GÄSTER: telefon som text för att behålla '+'. E-post är unik.
CREATE TABLE guests (
    id         INT AUTO_INCREMENT PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name  VARCHAR(50) NOT NULL,
    email      VARCHAR(100) NOT NULL,
    phone      VARCHAR(20),
    CONSTRAINT uq_guests_email UNIQUE (email)
);

-- BOKNINGAR: 1 gäst + 1 rum. Datum som DATE (in- och utcheckningstider är en regel för boendet).
-- Att antal gäster ryms i rummet och att bokningar inte överlappar kan inte
-- uttryckas med CHECK (kräver andra rader/tabeller) – det kontrolleras i applikationen.
CREATE TABLE bookings (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    guests_id        INT NOT NULL,
    rooms_id         INT NOT NULL,
    number_of_guests INT NOT NULL,
    check_in         DATE NOT NULL,
    check_out        DATE NOT NULL,
    CONSTRAINT fk_bookings_guests FOREIGN KEY (guests_id) REFERENCES guests(id),
    CONSTRAINT fk_bookings_rooms  FOREIGN KEY (rooms_id)  REFERENCES rooms(id),
    CONSTRAINT chk_dates  CHECK (check_out > check_in),
    CONSTRAINT chk_guests CHECK (number_of_guests >= 1)
);

-- =========================================
-- TESTDATA
-- =========================================
-- 5 orter
INSERT INTO location (city) VALUES
('Malé'), ('Masai Mara'), ('Los Angeles'), ('Mallorca'), ('Fairbanks');

-- 7 boenden. Mallorca har tre boenden med olika omdöme, faciliteter och avstånd,
-- så att filter och sortering på omdöme går att demonstrera.
INSERT INTO accommodations (name, city_id, pool, restaurant, beach_distance, city_distance, rating, description) VALUES
('Blue Manta Liveaboard', 1, FALSE, TRUE, 0, 5000, 5, 'Dykbåt runt Maldiverna, full utrustning att hyra'),
('Safari Adventure', 2, FALSE, TRUE, 7000, 2000, 5, 'Bo i lyxiga tält och utforska nationalparken från jeep med kunnig guide.'),
('Bel Air Mansion', 3, TRUE, TRUE, 1000, 0, 4, 'Dröm dig tillbaka till Fresh Prince of Bel Air tiden genom att bo i huset där serien spelades in. Detta boende kommer med privat kock.'),
('Gecko Hotel', 4, TRUE, TRUE, 2000, 4000, 3, 'En klassisk familjesemester med härliga aktiviteter för skrikiga ungar överallt. Oändligt med förfriskningar finns för trötta föräldrar'),
('Slumber Lodge', 5, FALSE, FALSE, 10000, 500, 2, 'Bo i en timmerstuga nära till staden och vandringsleder. Inget rinnande vatten eller elektricitet finns.'),
('Playa Sol', 4, FALSE, TRUE, 150, 2500, 4, 'Strandnära hotell med tapasrestaurang och havsutsikt.'),
('Casa Vieja', 4, TRUE, FALSE, 3500, 200, 5, 'Lugnt boutiquehotell i gamla stan med takpool.');

-- 26 rum med kapacitet 1–8
INSERT INTO rooms (accommodation_id, price_per_night, capacity, room_number) VALUES
(1, 500, 2, 'Hytt 1'), (1, 1000, 4, 'Hytt 2'), (1, 2000, 6, 'Hytt 3'), (1, 1100, 2, 'Hytt 4'),
(2, 900, 3, 'Tält 1'), (2, 700, 1, 'Tält 4'), (2, 1400, 4, 'Tält 7'), (2, 1000, 2, 'Tält 8'),
(3, 10000, 8, 'Huvudbyggnad'), (3, 3000, 2, 'Gästhus'), (3, 1000, 1, 'Bod'),
(4, 200, 4, 'Rum 201'), (4, 100, 2, 'Rum 312'), (4, 400, 5, 'Rum 510'), (4, 600, 7, 'Rum 623'), (4, 800, 2, 'Rum 711'),
(5, 50, 1, 'Stuga 2'), (5, 100, 4, 'Stuga 3'), (5, 150, 8, 'Stuga 5'), (5, 100, 2, 'Stuga 7'),
(6, 250, 1, 'Rum 1'), (6, 350, 2, 'Rum 2'), (6, 500, 4, 'Rum 3'),
(7, 150, 2, 'Suite A'), (7, 900, 2, 'Suite B'), (7, 1500, 4, 'Suite C');

-- 5 gäster
INSERT INTO guests (first_name, last_name, email, phone) VALUES
('Sara', 'Gonzales', 'sara@mail.com', '+5009874876'),
('Greg', 'Gonzales', 'greggy@mail.com', '+5046389856'),
('Harry', 'Hanson', 'harry@mail.com', '+4687531038'),
('Allan', 'Stanley', 'allst@mail.com', '+4809876374'),
('Texas', 'Ranger', 'texmex@mail.com', '+5567452354');

-- 26 bokningar i december 2026 (AI-genererade).
-- Några börjar samma dag som föregående slutar, för att testa att utcheckningsdagen inte upptar rummet.
INSERT INTO bookings (guests_id, rooms_id, number_of_guests, check_in, check_out) VALUES
(1, 1, 2, '2026-12-01', '2026-12-05'),
(2, 1, 1, '2026-12-05', '2026-12-10'),
(3, 1, 2, '2026-12-20', '2026-12-27'),
(4, 2, 4, '2026-12-03', '2026-12-08'),
(5, 3, 6, '2026-12-10', '2026-12-17'),
(1, 4, 2, '2026-12-22', '2026-12-29'),
(2, 5, 3, '2026-12-01', '2026-12-06'),
(3, 6, 1, '2026-12-06', '2026-12-09'),
(4, 7, 4, '2026-12-12', '2026-12-18'),
(5, 8, 2, '2026-12-18', '2026-12-22'),
(1, 9, 8, '2026-12-23', '2026-12-31'),
(2, 10, 2, '2026-12-02', '2026-12-04'),
(3, 10, 2, '2026-12-04', '2026-12-07'),
(4, 11, 1, '2026-12-14', '2026-12-15'),
(5, 12, 3, '2026-12-01', '2026-12-08'),
(1, 13, 2, '2026-12-08', '2026-12-12'),
(2, 14, 5, '2026-12-19', '2026-12-26'),
(3, 15, 7, '2026-12-24', '2026-12-31'),
(4, 16, 2, '2026-12-05', '2026-12-09'),
(5, 16, 1, '2026-12-09', '2026-12-13'),
(1, 17, 1, '2026-12-15', '2026-12-18'),
(2, 18, 4, '2026-12-10', '2026-12-14'),
(3, 19, 6, '2026-12-27', '2027-01-02'),
(4, 20, 2, '2026-12-01', '2026-12-03'),
(5, 20, 2, '2026-12-03', '2026-12-06'),
(1, 2, 3, '2026-12-08', '2026-12-11');

-- =========================================
-- FRÅGOR (exempel som provats i databasklienten innan de användes i C#)
-- =========================================
/* SÖK LEDIGA RUM: alla rum i Malé med plats för minst 2 personer
   som är lediga hela perioden 3–7/12.
   JOIN kopplar rum -> boende -> ort via främmande nycklar.
   Subfrågan hittar rum med bokningar som krockar (börjar före 7/12 och slutar efter 3/12),
   och NOT IN tar bort dem. Villkoret är check_out > och inte >=, så en bokning som
   checkar ut 3/12 krockar inte, eftersom utcheckningsdagen inte upptar rummet. */
SELECT r.room_number, a.name, l.city, r.price_per_night, r.capacity, a.rating
FROM rooms r
JOIN accommodations a ON r.accommodation_id = a.id
JOIN location l ON a.city_id = l.id
WHERE l.city = 'Malé'
  AND r.capacity >= 2
  AND r.id NOT IN (SELECT rooms_id FROM bookings
                   WHERE check_in < '2026-12-07' AND check_out > '2026-12-03');

/* SÖK MED FILTER: lediga rum på Mallorca för minst 4 personer 3–7/12,
   med pool, restaurang och max 3000 m till stranden, sorterat på pris (billigast först). */
SELECT r.room_number, a.name, l.city, r.price_per_night, r.capacity, a.rating
FROM rooms r
JOIN accommodations a ON r.accommodation_id = a.id
JOIN location l ON a.city_id = l.id
WHERE l.city = 'Mallorca'
  AND r.capacity >= 4
  AND r.id NOT IN (SELECT rooms_id FROM bookings
                   WHERE check_in < '2026-12-07' AND check_out > '2026-12-03')
  AND a.pool = TRUE
  AND a.restaurant = TRUE
  AND a.beach_distance <= 3000
ORDER BY r.price_per_night;

-- Snittpris per bokning (antal nätter × pris per natt)
SELECT ROUND(AVG(DATEDIFF(check_out, check_in) * price_per_night)) AS snittpris
FROM bookings
JOIN rooms ON bookings.rooms_id = rooms.id;
