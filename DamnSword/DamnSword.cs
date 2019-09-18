using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using Jypeli.Effects;

/// @author nimakarj
/// @version 0.1
/// <summary>
/// Damn Sword!
/// </summary>
public class DamnSword : PhysicsGame
{
    
Image taustaKuva = LoadImage("tausta1");

    const double KENTANLEVEYS = 5000;
    const double KENTANKORKEUS = 1080;

    const int RUUDUN_KOKO = 50;

    const double RUUDUN_LEVEYS = KENTANLEVEYS / 172;
    const double RUUDUN_KORKEUS = KENTANKORKEUS / 24;

    PlatformCharacter sankari;
    Image pelaajanKuva = LoadImage("pelaaja1");
    Image[] skini = { LoadImage("vihu1"), LoadImage("vihu2") };

    IntMeter pisteet;

    /// <summary>
    /// Pelin aloitus
    /// </summary>
    public override void Begin()
    {
        LuoKentta();

        Valikko();

        LisaaLaskuri();

        LisaaNappaimet();
    }

    /// <summary>
    /// Luodaan alkuvalikko
    /// </summary>
    public void Valikko()
    {
        MultiSelectWindow alkuValikko = new MultiSelectWindow("          Damn Sword! \n(without an actual sword)", "Start");
        alkuValikko.Color = Color.BloodRed;
        Add(alkuValikko);
    }

    /// <summary>
    /// Luodaan pelikenttä
    /// </summary>
    public void LuoKentta()
    {
        Gravity = new Vector(0, -1000);

        TileMap kentta = TileMap.FromLevelAsset("kentta");
        kentta.SetTileMethod('k', LuoReuna, Color.Transparent);
        kentta.SetTileMethod('l', LuoLattia);
        kentta.SetTileMethod('s', LuoReuna, Color.SlateGray);
        kentta.SetTileMethod('p', Sankari);
        kentta.SetTileMethod('v', LuoVihu, 4);
        kentta.SetTileMethod('m', Maali);
        kentta.Execute(RUUDUN_LEVEYS, RUUDUN_KORKEUS);

        GameObject tausta = new GameObject(10000, 1080);
        tausta.Image = taustaKuva;
        Add(tausta, -3);
        Layers[-3].RelativeTransition = new Vector(0.5, 0.5);

        Camera.ZoomFactor = 0.8;
        Camera.FollowX(sankari);
        Camera.FollowOffset = new Vector(Screen.Width / 2.5 - RUUDUN_KOKO, 0.0);
        Camera.StayInLevel = true;
    }

    /// <summary>
    /// Luodaan rohkea sankarimme
    /// </summary>
    /// <param name="paikka">Sijainti alussa</param>
    /// <param name="leveys">Sankarin koko</param>
    /// <param name="korkeus">Sankarin koko</param>
    public void Sankari(Vector paikka, double leveys, double korkeus)
    {
        sankari = new PlatformCharacter(leveys, korkeus, Shape.Circle);
        sankari.Color = Color.BrownGreen;
        sankari.Mass = 100.0;
        sankari.Image = pelaajanKuva;
        sankari.CanMoveOnAir = true;
        sankari.TurnsWhenWalking = true;
        sankari.Position = paikka;
        AddCollisionHandler(sankari, "vihunKivi", CollisionHandler.ExplodeBoth(100, true));
        sankari.Destroyed += delegate
        {
            Label loppu = new Label(500, 300, "You ded!");
            loppu.TextColor = Color.BloodRed;
            loppu.Color = Color.Black;
            Add(loppu);
        };
        Add(sankari);
    }

    /// <summary>
    /// Liikuttaa hahmoa annatuilla parametereillä oikealle ja vasemmall
    /// </summary>
    /// <param name="sankari">Pelin sankari</param>
    /// <param name="suunta">Pääohjelmasta tuotu kävelysuunta</param>
    public void LiikutaHahmoa(PlatformCharacter sankari, double suunta)
    {
        sankari.Walk(suunta);
    }

    /// <summary>
    /// Sankari hyppää
    /// </summary>
    /// <param name="sankari">Urhea sankarimme</param>
    /// <param name="korkeus"></param>
    public void Hyppaa(PlatformCharacter sankari, double korkeus)
    {
        sankari.Jump(korkeus);
    }

    /// <summary>
    /// Luodaan urhealle sankarillemme vihollisia
    /// </summary>
    /// <param name="paikka">Vihollisen sijainti</param>
    /// <param name="leveys">Vihollisen koko</param>
    /// <param name="korkeus">Vihollisen koko</param>
    /// <param name="liikemaara">Vihollisen liikkumasäde</param>
    public void LuoVihu(Vector paikka, double leveys, double korkeus, int liikemaara)
    {
        PhysicsObject vihu = new PhysicsObject(leveys, korkeus);
        vihu.Position = paikka;
        vihu.CanRotate = false;
        vihu.Image = VihunSkini();
        Add(vihu);

        PathFollowerBrain pfb = new PathFollowerBrain();
        List<Vector> reitti = new List<Vector>();
        reitti.Add(vihu.Position);
        Vector seuraavaPiste = new Vector(vihu.X - liikemaara * RUUDUN_LEVEYS, vihu.Y);
        reitti.Add(seuraavaPiste);
        pfb.Path = reitti;

        AddCollisionHandler(vihu, "pelaajanKivi", CollisionHandler.ExplodeBoth(50, true));

        Timer heittoAjastin = new Timer();
        Random rnd = new Random();
        heittoAjastin.Interval = rnd.Next(2, 8);
        heittoAjastin.Timeout += delegate () { Heita(vihu, "vihunKivi", -1000); } ;
        heittoAjastin.Start();
        vihu.Destroyed += delegate
        {
            heittoAjastin.Stop();
            pisteet.Value += 1;
        };

        vihu.Brain = pfb;
        pfb.Loop = true;
    }

    /// <summary>
    /// Vihollisen ulkoasu
    /// </summary>
    /// <returns>Palauttaa erilaisia ulkoasuja</returns>
    public Image VihunSkini()
    {
        return RandomGen.SelectOne(skini);
    }

    /// <summary>
    /// Kiviä tässä heitellään, vaikka piti olla miekkoja
    /// </summary>
    /// <param name="heittavaOlio">Kuka heittää</param>
    /// <param name="tagi">Mitä heitetään</param>
    /// <param name="suunta">Mihin suuntaan heitetään</param>
    public void Heita(PhysicsObject heittavaOlio, string tagi, int suunta)
    {
        PhysicsObject heitettava = new PhysicsObject(RUUDUN_LEVEYS / 3, RUUDUN_KORKEUS / 3);
        heitettava.Image = LoadImage("kivi");
        Add(heitettava);
        heitettava.Position = heittavaOlio.Position;
        heitettava.Hit(new Vector(suunta, 200));
        heitettava.Tag = tagi;
        heitettava.MaximumLifetime = TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// Luodaan pelikenttään lattia
    /// </summary>
    /// <param name="paikka">Sijainti</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    public void LuoLattia(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject lattia = new PhysicsObject(leveys, korkeus);
        lattia.Position = paikka;
        lattia.MakeStatic();
        lattia.Image = LoadImage("lattia");
        lattia.CollisionIgnoreGroup = 2;
        Add(lattia);
    }

    /// <summary>
    /// Luodaan reuna pelikenttään
    /// </summary>
    /// <param name="paikka">Sijainti</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    public void LuoReuna(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject katto = new PhysicsObject(leveys, korkeus);
        katto.Position = paikka;
        katto.MakeStatic();
        katto.Color = vari;
        Add(katto);
    }

    /// <summary>
    /// Luodaan kentän lopettava maali
    /// </summary>
    /// <param name="paikka">Sijainti</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    public void Maali(Vector paikka, double leveys, double korkeus)
 
   {
        PhysicsObject maali = new PhysicsObject(leveys, korkeus);
        maali.Position = paikka;
        maali.MakeStatic();
        maali.Image = LoadImage("maali");
        AddCollisionHandler(sankari, maali, CollisionHandler.DestroyBoth);
        maali.Destroyed += delegate
        {
            Label loppu = new Label(500, 300, "You won!");
            Add(loppu);
        };
        Add(maali);
    }

    /// <summary>
    /// Peliä ohjataan näppäimillä
    /// </summary>
    public void LisaaNappaimet()
    {
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaHahmoa, "Liikuta sankaria vasemmalle", sankari, -300.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaHahmoa, "Liikuta sankaria oikealle", sankari, 300.0);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Hyppää", sankari, 800.0);
        Keyboard.Listen(Key.A, ButtonState.Pressed, Heita, "Nakkaa kivi vasemmalle", sankari, "pelaajanKivi", -500);
        Keyboard.Listen(Key.S, ButtonState.Pressed, Heita, "Nakkaa kivi oikealle", sankari, "pelaajanKivi", 500);
        //Keyboard.Listen(Key.A, ButtonState.Pressed, NostaMiekkaa, "Nosta sitä miekkaa!", miekka, new Vector(2000, 5000));
        //Keyboard.Listen(Key.S, ButtonState.Pressed, IskeMiekalla, "Strike down upon thee with great vengeance", miekka, new Vector(20000, -50000));

        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    /// <summary>
    /// Pistelaskuri
    /// </summary>
    public void LisaaLaskuri()
    {
        pisteet = LuoPistelaskuri(Screen.Right - 100, Screen.Top - 100);
    }

    /// <summary>
    /// Luodaan pistelaskuri
    /// </summary>
    /// <param name="x">Sijainti x-akselilla</param>
    /// <param name="y">Sijainti y-akselilla</param>
    /// <returns></returns>
    public IntMeter LuoPistelaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0);

        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.White;
        naytto.BorderColor = Color.DarkRed;
        naytto.Color = Color.Black;
        Add(naytto);

        return laskuri;
    }

    
}
