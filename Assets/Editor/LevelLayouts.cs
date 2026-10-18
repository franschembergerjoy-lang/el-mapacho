using System.Collections.Generic;
using UnityEngine;

namespace RobaYCorre.EditorTools
{
    /// <summary>
    /// Diseño de los cinco niveles. Cada mapa es una cuadrícula de 1 m: '#' pared, letra = habitación,
    /// 'X' salida. Coordenadas del mundo: x = columna, z = (filas - 1 - fila).
    /// </summary>
    public static class LevelLayouts
    {
        static Dictionary<char, Material> Floors(bool mansion)
        {
            var tiles = MatLib.Floor("AzulejoCocina", MatLib.FloorStyle.Tiles, new Color(0.62f, 0.7f, 0.8f), new Color(0.44f, 0.5f, 0.6f));
            var wood = MatLib.Floor("Madera", MatLib.FloorStyle.Wood, new Color(0.82f, 0.6f, 0.38f), new Color(0.55f, 0.37f, 0.22f));
            var woodDark = MatLib.Floor("MaderaOscura", MatLib.FloorStyle.Wood, new Color(0.62f, 0.42f, 0.27f), new Color(0.38f, 0.24f, 0.14f));
            var woodLight = MatLib.Floor("MaderaClara", MatLib.FloorStyle.Wood, new Color(0.9f, 0.74f, 0.52f), new Color(0.66f, 0.5f, 0.32f));
            var carpetBlue = MatLib.Floor("MoquetaAzul", MatLib.FloorStyle.Carpet, new Color(0.55f, 0.62f, 0.88f), new Color(0.42f, 0.48f, 0.75f));
            var carpetYellow = MatLib.Floor("MoquetaAmarilla", MatLib.FloorStyle.Carpet, new Color(0.96f, 0.82f, 0.48f), new Color(0.85f, 0.68f, 0.35f));
            var carpetRed = MatLib.Floor("MoquetaRoja", MatLib.FloorStyle.Carpet, new Color(0.78f, 0.34f, 0.4f), new Color(0.6f, 0.24f, 0.3f));
            var bath = MatLib.Floor("AzulejoBano", MatLib.FloorStyle.Tiles, new Color(0.92f, 0.94f, 0.97f), new Color(0.68f, 0.74f, 0.82f));
            var green = MatLib.Floor("AzulejoVerde", MatLib.FloorStyle.Tiles, new Color(0.66f, 0.82f, 0.66f), new Color(0.45f, 0.6f, 0.48f));
            var concrete = MatLib.Floor("Cemento", MatLib.FloorStyle.Concrete, new Color(0.62f, 0.62f, 0.64f), new Color(0.45f, 0.45f, 0.48f));
            var marble = MatLib.Floor("Marmol", MatLib.FloorStyle.Checker, new Color(0.95f, 0.94f, 0.92f), new Color(0.28f, 0.27f, 0.32f));
            return new Dictionary<char, Material>
            {
                {'K', tiles}, {'L', wood}, {'B', mansion ? carpetRed : carpetBlue}, {'H', woodDark}, {'T', bath}, {'W', green}, {'P', green},
                {'N', carpetYellow}, {'S', woodLight}, {'O', woodLight}, {'G', carpetRed}, {'A', concrete}, {'C', marble}, {'F', marble},
                {'R', woodDark}, {'D', wood},
            };
        }

        static readonly Color Warm = new Color(1f, 0.85f, 0.62f);
        static readonly Color Cool = new Color(0.75f, 0.85f, 1f);

        public static List<LevelDef> All()
        {
            return new List<LevelDef> { Level1(), Level2(), Level3(), Level4(), Level5() };
        }

        // =====================================================================================
        static LevelDef Level1()
        {
            return new LevelDef
            {
                number = 1,
                sceneName = GameProgress.LevelScenes[0],
                title = GameProgress.LevelTitles[0],
                intro = "Una casita tranquila. El dueño duerme a pierna suelta en el dormitorio.\nRoba comida de la cocina y no hagas demasiado ruido.",
                goal = 25,
                tutorial = true,
                wallMat = "ParedNaranja",
                capMat = "RemateMarron",
                start = new Vector2(15f, 1.1f),
                floors = Floors(false),
                map = new[]
                {
                    "###################",
                    "#BBBBBB#TTTT#WWWWW#",
                    "#BBBBBB#TTTT#WWWWW#",
                    "#BBBBBB#TTTT#WWWWW#",
                    "#BBBBBB#TTTT#WWWWW#",
                    "###BB####TT####W###",
                    "#HHHHHHHHHHHHHHHHH#",
                    "#HHHHHHHHHHHHHHHHH#",
                    "###LLL######KKK####",
                    "#LLLLLLLL#KKKKKKKK#",
                    "#LLLLLLLL#KKKKKKKK#",
                    "#LLLLLLLLKKKKKKKKK#",
                    "#LLLLLLLLKKKKKKKKK#",
                    "#LLLLLLLL#KKKKKKKK#",
                    "###############X###",
                },
                populate = c =>
                {
                    // Dormitorio
                    c.Place("Cama", 3.5f, 12.65f, 180f);
                    c.Place("MesitaNoche", 1.95f, 13.45f, 180f);
                    c.Place("MesitaNoche", 5.05f, 13.45f, 180f);
                    c.Loot("Reloj", 1.95f, 13.5f, 0.55f);
                    c.Loot("Telefono", 5.05f, 13.5f, 0.55f);
                    c.Place("Armario", 0.55f, 10.6f, 90f);
                    c.Place("Alfombra", 3.5f, 10.4f);
                    c.Place("Planta", 6.4f, 9.6f);
                    c.Place("Lampara", 6.4f, 13.4f);
                    c.Resident("Habitante_Pijama", 3.5f, 12.65f, 180f, true, new Vector2(5.2f, 11.6f), 50f);
                    // Baño
                    c.Place("Banera", 8.6f, 13.35f, 180f);
                    c.Place("Inodoro", 11.3f, 13.45f, 180f);
                    c.Place("Lavabo", 11.55f, 11.0f, -90f);
                    c.Place("PlantaPequena", 7.6f, 9.6f);
                    // Lavadero
                    c.Place("Lavadora", 13.0f, 13.45f, 180f);
                    c.Place("Lavadora", 13.75f, 13.45f, 180f);
                    c.Place("Comoda", 16.6f, 13.55f, 180f);
                    c.Place("CestoRopa", 16.9f, 12.6f);
                    c.Place("RopaSuelo", 15.2f, 11.2f);
                    // Pasillo
                    c.Place("Cortina", 17.8f, 7.5f, -90f);
                    c.Place("Comoda", 6.0f, 8.57f, 180f);
                    c.Place("Cuadro", 6.0f, 8.82f, 180f);
                    c.Place("Planta", 0.6f, 8.4f);
                    // Salón
                    c.Place("MuebleTV", 0.45f, 3.0f, 90f);
                    c.Place("Sofa", 5.4f, 3.0f, -90f);
                    c.Place("Alfombra", 3.0f, 3.0f, 90f);
                    c.Place("MesaCentro", 3.0f, 3.0f, 90f);
                    c.Loot("PapasFritas", 3.0f, 3.2f, 0.4f);
                    c.Loot("Juguete", 2.2f, 1.4f);
                    c.Place("Estanteria", 7.6f, 5.6f, 180f);
                    c.Place("Sillon", 1.3f, 5.0f, 135f);
                    c.Place("Lampara", 0.8f, 0.8f);
                    c.Place("Planta", 8.3f, 0.7f);
                    // Cocina
                    c.Place("Encimera", 9.75f, 5.5f, 180f);
                    c.Place("Fogones", 10.7f, 5.5f, 180f);
                    c.Place("Fregadero", 15.5f, 5.5f, 180f);
                    c.Place("Encimera", 16.5f, 5.5f, 180f);
                    c.Place("Nevera", 17.4f, 5.45f, 180f);
                    c.Loot("Galletas", 9.75f, 5.5f, 0.91f);
                    c.Loot("Manzana", 16.5f, 5.5f, 0.91f);
                    c.Place("MesaCocina", 13.5f, 2.8f);
                    c.Place("Silla", 12.45f, 2.8f, 90f);
                    c.Place("Silla", 14.55f, 2.8f, -90f);
                    c.Loot("Queso", 13.15f, 2.85f, 0.76f);
                    c.Loot("Pastel", 13.85f, 2.75f, 0.76f);
                    c.Place("Papelera", 9.7f, 0.6f);
                    c.Place("PlantaPequena", 17.3f, 0.7f);
                    // Luces
                    c.PointLight(13.5f, 2.4f, 2.8f, Cool, 0.9f, 7f);
                    c.PointLight(9f, 2.2f, 7.5f, Warm, 0.6f, 8f);
                    c.PointLight(9.5f, 2.2f, 11.5f, Cool, 0.5f, 5f);
                    c.PointLight(15f, 2.2f, 11.5f, Cool, 0.5f, 5f);
                }
            };
        }

        // =====================================================================================
        static LevelDef Level2()
        {
            return new LevelDef
            {
                number = 2,
                sceneName = GameProgress.LevelScenes[1],
                title = GameProgress.LevelTitles[1],
                intro = "Aquí vive alguien muy desordenado... y con el sueño ligero. Hay sillas y lámparas por todas partes\ny jarrones que se rompen si pasas corriendo. Si oye algo, vendrá a investigar.",
                goal = 55,
                wallMat = "ParedTurquesa",
                capMat = "RemateMarron",
                start = new Vector2(4f, 1.1f),
                floors = Floors(false),
                map = new[]
                {
                    "#######################",
                    "#NNNNNN#BBBBBBB#TTTTTT#",
                    "#NNNNNN#BBBBBBB#TTTTTT#",
                    "#NNNNNN#BBBBBBB#TTTTTT#",
                    "#NNNNNN#BBBBBBB#TTTTTT#",
                    "#NNNNNN#BBBBBBB#TTTTTT#",
                    "###NN#####BB######TT###",
                    "#HHHHHHHHHHHHHHHHHHHHH#",
                    "#HHHHHHHHHHHHHHHHHHHHH#",
                    "####LLL#####KK####PP###",
                    "#LLLLLLLLLL#KKKKK#PPPP#",
                    "#LLLLLLLLLL#KKKKK#PPPP#",
                    "#LLLLLLLLLLKKKKKK#PPPP#",
                    "#LLLLLLLLLLKKKKKKPPPPP#",
                    "#LLLLLLLLLL#KKKKKPPPPP#",
                    "#LLLLLLLLLL#KKKKK#PPPP#",
                    "####X##################",
                },
                populate = c =>
                {
                    // Cuarto infantil
                    c.Place("CamaInfantil", 1.9f, 14.8f, 180f);
                    c.Place("BaulJuguetes", 5.8f, 15.5f, 180f);
                    c.Place("JuguetesSuelo", 4.0f, 12.0f);
                    c.Place("JuguetesSuelo", 5.2f, 13.6f, 70f);
                    c.Place("Planta", 6.4f, 10.6f);
                    c.Place("Lampara", 0.6f, 10.6f);
                    c.Loot("Juguete", 4.4f, 12.7f);
                    c.Loot("PapasFritas", 1.5f, 11.2f);
                    // Dormitorio
                    c.Place("Cama", 11.0f, 14.65f, 180f);
                    c.Place("MesitaNoche", 9.55f, 15.4f, 180f);
                    c.Place("MesitaNoche", 12.45f, 15.4f, 180f);
                    c.Place("Armario", 14.47f, 12.0f, -90f);
                    var dresser = c.Place("Comoda", 7.43f, 12.0f, 90f);
                    c.FragileOn(dresser, "Jarron", 7.43f, 12.3f, 0.8f, "la cómoda");
                    c.Loot("Reloj", 7.43f, 11.7f, 0.8f);
                    c.Place("RopaSuelo", 8.6f, 11.0f);
                    c.Place("RopaSuelo", 12.8f, 10.9f, 120f);
                    c.Loot("Telefono", 12.45f, 15.45f, 0.55f);
                    c.Loot("Camara", 9.55f, 15.45f, 0.55f);
                    c.Resident("Habitante_Bata", 11.0f, 14.65f, 180f, true, new Vector2(13.2f, 13.0f), 32f);
                    // Baño
                    c.Place("Banera", 16.95f, 15.35f, 180f);
                    c.Loot("Juguete", 16.95f, 15.35f, 0.48f);
                    c.Place("Inodoro", 21.3f, 15.45f, 180f);
                    c.Place("Lavabo", 21.55f, 12.5f, -90f);
                    c.Place("CestoRopa", 16.0f, 11.0f);
                    // Pasillo
                    c.Place("Cortina", 0.2f, 8.5f, 90f);
                    var console = c.Place("Comoda", 6.5f, 9.57f, 180f);
                    c.FragileOn(console, "Jarron", 6.5f, 9.6f, 0.8f, "el mueble del pasillo");
                    c.Place("Cajas", 9.0f, 9.3f);
                    c.Place("Lampara", 15.0f, 9.45f);
                    c.Place("Silla", 12.5f, 8.0f, 30f);
                    c.Place("Silla", 18.0f, 7.7f, -60f);
                    c.Place("Planta", 21.4f, 7.6f);
                    // Salón comedor
                    c.Place("MuebleTV", 8.5f, 6.6f, 180f);
                    c.Place("SofaTurquesa", 8.5f, 3.6f);
                    c.Place("Alfombra", 8.5f, 4.6f);
                    c.Place("MesaCentro", 8.5f, 5.2f);
                    c.Loot("Galletas", 8.5f, 5.2f, 0.4f);
                    var dining = c.Place("MesaComedor", 3.0f, 4.0f);
                    c.Place("Silla", 3.0f, 4.95f, 180f);
                    c.Place("Silla", 3.0f, 3.05f, 0f);
                    c.Place("Silla", 1.6f, 2.0f, 70f);
                    c.Loot("Pastel", 2.65f, 4.0f, 0.76f);
                    c.FragileOn(dining, "Platos", 3.45f, 4.0f, 0.76f, "la mesa");
                    c.Place("Estanteria", 0.4f, 1.6f, 90f);
                    c.Place("Cajas", 6.4f, 1.0f, 10f);
                    c.Place("Lampara", 10.4f, 6.4f);
                    c.Place("RopaSuelo", 5.5f, 2.6f, 45f);
                    // Cocina
                    c.Place("Encimera", 14.4f, 6.5f, 180f);
                    c.Place("Fogones", 15.3f, 6.5f, 180f);
                    c.Place("Fregadero", 16.2f, 6.5f, 180f);
                    c.Place("Nevera", 11.55f, 5.6f, 90f);
                    c.Loot("Queso", 14.7f, 6.5f, 0.91f);
                    c.Place("MesaCocina", 14.2f, 3.0f);
                    c.Place("Silla", 14.2f, 4.0f, 180f);
                    c.Place("Silla", 14.2f, 2.0f, 0f);
                    c.Loot("PavoAsado", 13.95f, 3.0f, 0.76f);
                    c.Loot("Manzana", 14.6f, 3.05f, 0.76f);
                    c.Place("Papelera", 11.6f, 0.6f);
                    // Despensa
                    c.Place("Estanteria", 21.0f, 6.6f, 180f);
                    c.Loot("Galletas", 21.0f, 6.62f, 0.97f);
                    c.Place("Lavadora", 21.47f, 4.2f, -90f);
                    c.Place("CestoRopa", 21.3f, 2.4f);
                    c.Place("Cajas", 18.4f, 0.8f);
                    c.Loot("PapasFritas", 19.6f, 3.4f);
                    // Luces
                    c.PointLight(14f, 2.4f, 3f, Cool, 0.8f, 6f);
                    c.PointLight(19.5f, 2.2f, 3.5f, Cool, 0.5f, 5f);
                    c.PointLight(11f, 2.2f, 8.5f, Warm, 0.6f, 9f);
                    c.PointLight(18.5f, 2.2f, 13f, Cool, 0.5f, 5f);
                    c.PointLight(3.5f, 2.2f, 13f, Warm, 0.5f, 5f);
                }
            };
        }

        // =====================================================================================
        static LevelDef Level3()
        {
            return new LevelDef
            {
                number = 3,
                sceneName = GameProgress.LevelScenes[2],
                title = GameProgress.LevelTitles[2],
                intro = "¡Cuidado con Toby! El perro patrulla el salón, el pasillo y el comedor. Si te huele, ladrará y despertará a todos.\nUsa los escondites y las rutas alternativas entre habitaciones.",
                goal = 90,
                wallMat = "ParedVerde",
                capMat = "RemateMarron",
                start = new Vector2(21f, 1.1f),
                floors = Floors(false),
                map = new[]
                {
                    "#########################",
                    "#BBBBBBBB#SSSSSS#TTTTTTT#",
                    "#BBBBBBBB#SSSSSS#TTTTTTT#",
                    "#BBBBBBBB#SSSSSS#TTTTTTT#",
                    "#BBBBBBBB#SSSSSS#TTTTTTT#",
                    "#BBBBBBBB#SSSSSS#TTTTTTT#",
                    "####BB#####SS#######TT###",
                    "#HHHHHHHHHHHHHHHHHHHHHHH#",
                    "#HHHHHHHHHHHHHHHHHHHHHHH#",
                    "###LLL########DD#####KK##",
                    "#LLLLLLLLLL#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#DDDDDDDKKKKK#",
                    "#LLLLLLLLLLDDDDDDDDKKKKK#",
                    "#LLLLLLLLLLDDDDDDD#KKKKK#",
                    "#LLLLLLLLLL#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#DDDDDD#KKKKK#",
                    "#####################X###",
                },
                populate = c =>
                {
                    // Dormitorio
                    c.Place("Cama", 4.5f, 16.65f, 180f);
                    c.Place("MesitaNoche", 3.25f, 17.4f, 180f);
                    c.Place("MesitaNoche", 5.75f, 17.4f, 180f);
                    c.Place("Armario", 0.55f, 13.6f, 90f);
                    c.Place("Comoda", 8.58f, 14.5f, -90f);
                    c.Place("Planta", 8.4f, 12.6f);
                    c.Loot("Telefono", 5.75f, 17.45f, 0.55f);
                    c.Loot("Reloj", 3.25f, 17.45f, 0.55f);
                    c.Loot("Joyero", 8.58f, 14.5f, 0.8f);
                    c.Resident("Habitante_Pijama", 4.5f, 16.65f, 180f, true, new Vector2(6.6f, 15.2f), 40f);
                    // Estudio
                    c.Place("Escritorio", 12.5f, 17.5f, 180f);
                    c.Place("Estanteria", 10.0f, 17.6f, 180f);
                    c.Place("Estanteria", 15.0f, 17.6f, 180f);
                    c.Place("Sillon", 14.6f, 13.2f, -135f);
                    c.Place("Lampara", 9.6f, 12.6f);
                    c.Loot("Camara", 12.25f, 17.45f, 0.76f);
                    c.Loot("Galletas", 12.8f, 17.45f, 0.76f);
                    // Baño
                    c.Place("Banera", 17.2f, 17.35f, 180f);
                    c.Loot("Juguete", 17.2f, 17.35f, 0.48f);
                    c.Place("Inodoro", 23.3f, 17.45f, 180f);
                    c.Place("Lavabo", 23.55f, 14.5f, -90f);
                    c.Place("CestoRopa", 18.0f, 13.0f);
                    // Pasillo
                    c.Place("Cortina", 23.8f, 10.5f, -90f);
                    var console = c.Place("Comoda", 8.0f, 11.57f, 180f);
                    c.FragileOn(console, "Jarron", 7.75f, 11.6f, 0.8f, "el mueble del pasillo");
                    c.Loot("Manzana", 8.3f, 11.55f, 0.8f);
                    c.Place("Planta", 0.6f, 11.4f);
                    c.Place("Cuadro", 16.5f, 11.82f, 180f);
                    // Salón
                    c.Place("MuebleTV", 8.5f, 8.6f, 180f);
                    c.Place("Sofa", 8.5f, 5.3f);
                    c.Place("MesaCentro", 8.5f, 6.9f);
                    c.Loot("PapasFritas", 8.5f, 6.9f, 0.4f);
                    c.Place("CamaPerro", 1.2f, 1.0f);
                    c.Place("Estanteria", 0.4f, 5.5f, 90f);
                    c.Place("Sillon", 2.0f, 7.6f, 135f);
                    c.Place("Lampara", 10.4f, 0.6f);
                    c.Place("Alfombra", 5.5f, 4.0f);
                    c.Loot("Juguete", 4.6f, 3.0f);
                    // Comedor
                    c.Place("MesaComedor", 14.5f, 4.0f, 90f);
                    c.Place("Silla", 13.4f, 4.0f, 90f);
                    c.Place("Silla", 15.6f, 4.0f, -90f);
                    var side = c.Place("Comoda", 12.3f, 8.57f, 180f);
                    c.FragileOn(side, "Platos", 12.0f, 8.6f, 0.8f, "el aparador");
                    c.Loot("Reloj", 12.6f, 8.55f, 0.8f);
                    c.Loot("Queso", 14.5f, 3.6f, 0.76f);
                    c.Loot("PavoAsado", 14.5f, 4.4f, 0.76f);
                    c.Place("Planta", 17.4f, 0.6f);
                    c.Place("Planta", 11.6f, 0.6f);
                    // Cocina
                    c.Place("Encimera", 23.5f, 6.5f, -90f);
                    c.Place("Fogones", 23.5f, 5.6f, -90f);
                    c.Place("Fregadero", 23.5f, 4.6f, -90f);
                    c.Place("Nevera", 18.55f, 2.5f, 90f);
                    c.Place("MesaCocina", 21.0f, 4.3f);
                    c.Loot("Galletas", 23.5f, 6.5f, 0.91f);
                    c.Loot("Manzana", 21.35f, 4.3f, 0.76f);
                    c.Loot("Pastel", 20.65f, 4.3f, 0.76f);
                    c.Place("Papelera", 23.4f, 0.6f);
                    // Perro
                    c.Dog(5f, 3f, new Vector2(5f, 3f), new Vector2(4.2f, 10.5f), new Vector2(14.5f, 10.5f), new Vector2(14.5f, 6.4f), new Vector2(15.8f, 1.5f), new Vector2(11f, 4.5f));
                    // Luces
                    c.PointLight(21f, 2.4f, 4f, Cool, 0.8f, 6f);
                    c.PointLight(14.5f, 2.4f, 4.5f, Warm, 0.7f, 7f);
                    c.PointLight(12f, 2.2f, 10.5f, Warm, 0.6f, 10f);
                    c.PointLight(20f, 2.2f, 15f, Cool, 0.5f, 5f);
                    c.PointLight(12.5f, 2.2f, 15f, Warm, 0.5f, 5f);
                }
            };
        }

        // =====================================================================================
        static LevelDef Level4()
        {
            return new LevelDef
            {
                number = 4,
                sceneName = GameProgress.LevelScenes[3],
                title = GameProgress.LevelTitles[3],
                intro = "Una casa moderna llena de tecnología: cámaras que giran y sensores de movimiento que se encienden y apagan.\nAgáchate para cruzar los sensores y espera a que las cámaras miren a otro lado. Aquí viven dos personas.",
                goal = 130,
                wallMat = "ParedGrisAzul",
                capMat = "RemateOscuro",
                start = new Vector2(24f, 1.1f),
                floors = Floors(false),
                map = new[]
                {
                    "###########################",
                    "#BBBBBBB#OOOOOO#GGGGGGGGGG#",
                    "#BBBBBBB#OOOOOO#GGGGGGGGGG#",
                    "#BBBBBBB#OOOOOO#GGGGGGGGGG#",
                    "#BBBBBBB#OOOOOO#GGGGGGGGGG#",
                    "#BBBBBBB#OOOOOO#GGGGGGGGGG#",
                    "###BB######OO#######GG#####",
                    "#HHHHHHHHHHHHHHHHHHHHHHHHH#",
                    "#HHHHHHHHHHHHHHHHHHHHHHHHH#",
                    "####LL#######KK#######AA###",
                    "#LLLLLLLLLLL#KKKKKKKK#AAAA#",
                    "#LLLLLLLLLLL#KKKKKKKK#AAAA#",
                    "#LLLLLLLLLLLKKKKKKKKK#AAAA#",
                    "#LLLLLLLLLLLKKKKKKKKK#AAAA#",
                    "#LLLLLLLLLLL#KKKKKKKK#AAAA#",
                    "#LLLLLLLLLLL#KKKKKKKKAAAAA#",
                    "#LLLLLLLLLLL#KKKKKKKKAAAAA#",
                    "#LLLLLLLLLLL#KKKKKKKK#AAAA#",
                    "########################X##",
                },
                populate = c =>
                {
                    // Dormitorio
                    c.Place("Cama", 4.0f, 16.65f, 180f);
                    c.Place("MesitaNoche", 2.75f, 17.4f, 180f);
                    c.Place("MesitaNoche", 5.25f, 17.4f, 180f);
                    c.Place("Armario", 7.47f, 14.0f, -90f);
                    c.Place("Planta", 0.6f, 12.6f);
                    c.Loot("Telefono", 5.25f, 17.45f, 0.55f);
                    c.Loot("Reloj", 2.75f, 17.45f, 0.55f);
                    c.Resident("Habitante_Pijama", 4.0f, 16.65f, 180f, true, new Vector2(6.2f, 15.0f), 38f);
                    // Oficina
                    c.Place("Escritorio", 11.5f, 17.5f, 180f);
                    c.Place("Estanteria", 9.0f, 17.6f, 180f);
                    c.Place("Estanteria", 14.6f, 15.0f, -90f);
                    c.Loot("Camara", 11.25f, 17.45f, 0.76f);
                    c.Loot("Telefono", 11.8f, 17.45f, 0.76f);
                    c.Cam(8.35f, 12.35f, 45f, 60f, 7f, 0.5f);
                    // Sala multimedia
                    c.Place("MuebleTV", 22.0f, 17.6f, 180f);
                    c.Place("SofaMorado", 22.0f, 15.4f);
                    c.Place("MesaCentro", 22.0f, 16.6f);
                    c.Loot("Pastel", 22.0f, 16.6f, 0.4f);
                    c.Place("Piano", 17.0f, 17.45f, 180f);
                    c.Place("Pedestal", 24.8f, 13.0f);
                    c.Loot("Joyero", 24.8f, 13.0f, 1.04f);
                    c.Place("Planta", 15.6f, 12.6f);
                    c.Place("Sillon", 18.0f, 14.0f, 45f);
                    c.Cam(25.6f, 17.6f, 225f, 70f, 8f, 0.55f);
                    c.Sensor(20.5f, 12.95f, 2.4f, 1.4f, 3f, 2f, 0.5f);
                    // Pasillo
                    c.Place("Cortina", 0.2f, 10.5f, 90f);
                    c.Place("Comoda", 10.0f, 11.57f, 180f);
                    c.Loot("Manzana", 10.0f, 11.6f, 0.8f);
                    c.Place("Planta", 25.4f, 9.6f);
                    c.Cam(16.0f, 11.65f, 180f, 120f, 7.5f, 0.45f);
                    // Salón
                    c.Place("Estanteria", 0.4f, 4.5f, 90f);
                    c.Loot("Camara", 0.4f, 4.5f, 0.97f);
                    c.Sensor(2.1f, 4.5f, 3.0f, 2.6f, 3f, 2f, 1.2f);
                    c.Place("MuebleTV", 9.5f, 8.6f, 180f);
                    c.Place("Sofa", 9.5f, 5.3f);
                    c.Place("MesaCentro", 9.5f, 6.9f);
                    c.Loot("PapasFritas", 9.5f, 6.9f, 0.4f);
                    c.Place("Lampara", 11.4f, 0.6f);
                    c.Place("Planta", 0.6f, 0.6f);
                    c.Place("Sillon", 5.5f, 1.5f, 20f);
                    // Cocina
                    c.Place("Encimera", 16.0f, 8.5f, 180f);
                    c.Place("Fogones", 17.0f, 8.5f, 180f);
                    c.Place("Fregadero", 18.0f, 8.5f, 180f);
                    c.Place("Nevera", 20.35f, 8.45f, 180f);
                    c.Place("Encimera", 16.5f, 4.5f);
                    c.Place("Encimera", 17.5f, 4.5f);
                    c.Loot("PavoAsado", 16.5f, 4.5f, 0.91f);
                    c.Loot("Queso", 17.5f, 4.5f, 0.91f);
                    c.Place("MesaCocina", 14.0f, 2.0f);
                    c.Loot("Galletas", 13.75f, 2.0f, 0.76f);
                    c.Loot("Manzana", 14.3f, 2.05f, 0.76f);
                    // Garaje
                    c.Place("Coche", 23.5f, 5.2f);
                    c.Place("Cajas", 21.9f, 1.0f);
                    c.Place("Cajas", 25.3f, 7.8f, 90f);
                    c.Place("CestoRopa", 25.3f, 2.6f);
                    c.Loot("Juguete", 25.2f, 4.8f);
                    // Habitante despierto que patrulla
                    var insomne = c.Resident("Habitante_Insomne", 16.5f, 2.5f, 0f, false, new Vector2(16.5f, 2.5f), 30f,
                        new Vector2(16.5f, 2.5f), new Vector2(12f, 5.5f), new Vector2(6f, 4f), new Vector2(4.5f, 10.5f), new Vector2(13.5f, 10.5f), new Vector2(13.5f, 6.5f));
                    insomne.walkSpeed = 1.4f;
                    // Luces
                    c.PointLight(16f, 2.4f, 4f, Cool, 0.8f, 7f);
                    c.PointLight(23.5f, 2.4f, 4f, Cool, 0.6f, 6f);
                    c.PointLight(13f, 2.2f, 10.5f, Cool, 0.6f, 12f);
                    c.PointLight(20f, 2.2f, 15f, new Color(0.7f, 0.5f, 1f), 0.6f, 7f);
                    c.PointLight(11.5f, 2.2f, 15f, Cool, 0.5f, 5f);
                    c.PointLight(5f, 2.2f, 4.5f, Warm, 0.5f, 7f);
                }
            };
        }

        // =====================================================================================
        static LevelDef Level5()
        {
            return new LevelDef
            {
                number = 5,
                sceneName = GameProgress.LevelScenes[4],
                title = GameProgress.LevelTitles[4],
                intro = "La mansión del coleccionista: mayordomo, ama de llaves, un perro, cámaras y sensores.\nEl joyero de la galería es la joya de la colección... y tiene alarma. Róbalo, escóndete y escapa con todo.",
                goal = 180,
                special = true,
                wallMat = "ParedBurdeos",
                capMat = "RemateDorado",
                mansionExit = true,
                start = new Vector2(16f, 1.1f),
                floors = Floors(true),
                map = new[]
                {
                    "#################################",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "#BBBBBBBBB#CCCCCCCCCCC#RRRRRRRRR#",
                    "####BB#########CCC#######RR######",
                    "#HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH#",
                    "#HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH#",
                    "###LL#########FFF#####DD#####KK##",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDDDKKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDDDKKKKK#",
                    "#LLLLLLLLLLFFFFFFFFFDDDDDD#KKKKK#",
                    "#LLLLLLLLLLFFFFFFFFFDDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "#LLLLLLLLLL#FFFFFFF#DDDDDD#KKKKK#",
                    "################X################",
                },
                populate = c =>
                {
                    // Dormitorio principal
                    c.Place("CamaReal", 5.0f, 19.65f, 180f);
                    c.Place("MesitaNoche", 3.6f, 20.4f, 180f);
                    c.Place("MesitaNoche", 6.4f, 20.4f, 180f);
                    c.Place("Armario", 0.55f, 16.5f, 90f);
                    c.Place("Comoda", 9.58f, 17.0f, -90f);
                    c.Place("Planta", 9.4f, 14.6f);
                    c.Place("AlfombraDorada", 5.0f, 16.4f);
                    c.Loot("Reloj", 3.6f, 20.45f, 0.55f);
                    c.Loot("Telefono", 6.4f, 20.45f, 0.55f);
                    c.Loot("Camara", 9.58f, 17.0f, 0.8f);
                    c.Resident("Coleccionista", 5.0f, 19.65f, 180f, true, new Vector2(7.4f, 18.0f), 35f);
                    // Galería
                    c.Place("Pedestal", 12.0f, 19.8f);
                    c.Loot("Reloj", 12.0f, 19.8f, 1.04f);
                    c.Place("Pedestal", 20.0f, 19.8f);
                    c.Loot("Camara", 20.0f, 19.8f, 1.04f);
                    c.Place("Pedestal", 16.0f, 19.4f);
                    c.Loot("Joyero", 16.0f, 19.4f, 1.04f, true, true, 0.01f);
                    c.Place("Pedestal", 13.5f, 16.2f);
                    c.Loot("Juguete", 13.5f, 16.2f, 1.04f);
                    c.Place("Pedestal", 18.5f, 16.2f);
                    c.Loot("Telefono", 18.5f, 16.2f, 1.04f);
                    c.Place("CuadroRetrato", 13.8f, 20.82f, 180f);
                    c.Place("Cuadro", 18.2f, 20.82f, 180f);
                    c.Place("CortinaVerde", 10.2f, 16.2f, 90f);
                    c.Place("Armario", 21.47f, 16.6f, -90f);
                    c.Place("Estatua", 10.75f, 20.3f);
                    c.Place("Estatua", 21.25f, 20.3f);
                    c.Sensor(16.0f, 18.3f, 3.6f, 1.6f, 3f, 2.5f, 0f);
                    c.Cam(16.0f, 20.65f, 180f, 100f, 7.5f, 0.5f);
                    // Biblioteca
                    c.Place("Estanteria", 23.2f, 20.6f, 180f);
                    c.Place("Estanteria", 24.6f, 20.6f, 180f);
                    c.Place("Estanteria", 29.4f, 20.6f, 180f);
                    c.Place("Estanteria", 30.8f, 20.6f, 180f);
                    c.Loot("Galletas", 30.8f, 20.62f, 0.97f);
                    c.Place("Escritorio", 27.0f, 20.5f, 180f);
                    c.Loot("Reloj", 26.75f, 20.45f, 0.76f);
                    c.Loot("PapasFritas", 27.3f, 20.45f, 0.76f);
                    c.Place("Sillon", 24.0f, 16.0f, 45f);
                    c.Place("Sillon", 30.0f, 16.0f, -45f);
                    c.Place("Chimenea", 31.57f, 17.5f, -90f);
                    c.Place("Lampara", 22.6f, 14.6f);
                    c.Sensor(25.5f, 15.0f, 2.2f, 1.4f, 2.5f, 2.5f, 1.2f);
                    c.Cam(31.6f, 20.6f, 225f, 60f, 8f, 0.5f);
                    // Pasillo
                    c.Place("CortinaVerde", 31.8f, 12.5f, -90f);
                    c.Place("Planta", 0.6f, 13.4f);
                    var c1 = c.Place("Comoda", 8.0f, 13.57f, 180f);
                    c.FragileOn(c1, "Jarron", 7.8f, 13.6f, 0.8f, "el mueble del pasillo");
                    c.Place("Comoda", 21.0f, 13.57f, 180f);
                    c.Loot("Manzana", 21.0f, 13.6f, 0.8f);
                    c.Place("CuadroRetrato", 11.5f, 13.82f, 180f);
                    c.Place("Cuadro", 27.5f, 13.82f, 180f);
                    c.Cam(12.5f, 13.65f, 180f, 110f, 7f, 0.45f);
                    // Salón
                    c.Place("Chimenea", 8.0f, 10.57f, 180f);
                    c.Place("Sofa", 8.0f, 7.3f);
                    c.Place("MesaCentro", 8.0f, 8.9f);
                    c.Loot("PapasFritas", 8.0f, 8.9f, 0.4f);
                    c.Place("Piano", 0.55f, 5.0f, 90f);
                    c.Loot("Telefono", 0.45f, 5.0f, 1.28f);
                    c.Place("Sillon", 2.5f, 8.5f, 135f);
                    c.Place("Sillon", 2.5f, 2.0f, 45f);
                    c.Place("CamaPerro", 9.8f, 1.0f);
                    c.Place("Lampara", 10.4f, 10.4f);
                    c.Place("Lampara", 0.6f, 0.6f);
                    c.Place("Alfombra", 6.0f, 5.0f);
                    c.Loot("Juguete", 5.0f, 4.0f);
                    // Recibidor
                    c.Place("AlfombraDorada", 15.0f, 5.0f, 90f);
                    c.Place("Planta", 11.6f, 0.6f);
                    c.Place("Planta", 18.4f, 0.6f);
                    c.Place("Armario", 11.55f, 2.5f, 90f);
                    c.Place("Estatua", 12.0f, 9.8f);
                    c.Place("Estatua", 18.0f, 9.8f);
                    // Comedor
                    c.Place("MesaComedor", 22.5f, 4.7f, 90f);
                    c.Place("MesaComedor", 22.5f, 6.3f, 90f);
                    c.Place("Silla", 21.4f, 4.7f, 90f);
                    c.Place("Silla", 21.4f, 6.3f, 90f);
                    c.Place("Silla", 23.6f, 4.7f, -90f);
                    c.Place("Silla", 23.6f, 6.3f, -90f);
                    var sb = c.Place("Comoda", 20.5f, 10.57f, 180f);
                    c.FragileOn(sb, "Platos", 20.5f, 10.6f, 0.8f, "el aparador");
                    c.Place("Comoda", 24.6f, 10.57f, 180f);
                    c.Loot("Reloj", 24.6f, 10.6f, 0.8f);
                    c.Loot("PavoAsado", 22.5f, 4.4f, 0.76f);
                    c.Loot("Pastel", 22.5f, 6.6f, 0.76f);
                    c.Loot("Queso", 22.5f, 5.5f, 0.76f);
                    c.Place("Planta", 25.4f, 0.6f);
                    // Cocina
                    c.Place("Encimera", 31.5f, 9.0f, -90f);
                    c.Place("Fogones", 31.5f, 8.1f, -90f);
                    c.Place("Fregadero", 31.5f, 7.2f, -90f);
                    c.Place("Encimera", 31.5f, 6.2f, -90f);
                    c.Place("Nevera", 31.45f, 4.9f, -90f);
                    c.Place("Encimera", 28.5f, 5.0f, 90f);
                    c.Place("Encimera", 28.5f, 6.0f, 90f);
                    c.Place("MesaCocina", 28.5f, 1.8f);
                    c.Loot("Galletas", 31.5f, 9.0f, 0.91f);
                    c.Loot("Manzana", 31.5f, 6.2f, 0.91f);
                    c.Loot("Queso", 28.5f, 5.0f, 0.91f);
                    c.Loot("PapasFritas", 28.5f, 6.0f, 0.91f);
                    c.Cam(31.6f, 0.4f, -45f, 60f, 7f, 0.5f);
                    // Habitantes y perro
                    var butler = c.Resident("Mayordomo", 15f, 6f, 0f, false, new Vector2(15f, 6f), 30f,
                        new Vector2(15f, 6f), new Vector2(15.5f, 12.5f), new Vector2(22.5f, 12.5f), new Vector2(22.5f, 8.8f),
                        new Vector2(26f, 7.5f), new Vector2(29f, 3.5f), new Vector2(24.5f, 2.5f), new Vector2(19f, 5.5f));
                    butler.walkSpeed = 1.5f;
                    var maid = c.Resident("Ama_de_llaves", 5f, 12.5f, 90f, false, new Vector2(5f, 12.5f), 30f,
                        new Vector2(5f, 12.5f), new Vector2(16f, 12.5f), new Vector2(16f, 16.8f), new Vector2(26f, 12.5f), new Vector2(27f, 17f), new Vector2(26f, 12.5f));
                    maid.walkSpeed = 1.4f;
                    c.Dog(5f, 5.5f, new Vector2(5f, 5.5f), new Vector2(3.5f, 12.4f), new Vector2(9f, 12.4f), new Vector2(6f, 8f), new Vector2(11f, 5.5f));
                    // Luces
                    c.PointLight(15f, 2.6f, 5f, Warm, 0.9f, 9f);
                    c.PointLight(22.5f, 2.5f, 5.5f, Warm, 0.8f, 7f);
                    c.PointLight(29f, 2.4f, 5f, Cool, 0.7f, 6f);
                    c.PointLight(16f, 2.4f, 12.5f, Warm, 0.6f, 14f);
                    c.PointLight(16f, 2.5f, 17.5f, new Color(1f, 0.9f, 0.7f), 0.9f, 8f);
                    c.PointLight(27f, 2.4f, 17.5f, Warm, 0.6f, 7f);
                    c.PointLight(5f, 2.4f, 17.5f, Warm, 0.5f, 6f);
                    c.PointLight(5f, 2.4f, 5f, Warm, 0.5f, 8f);
                }
            };
        }
    }
}
