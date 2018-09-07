using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml;
using System.Xaml.Schema;
using System.Globalization;
using System.Windows.Media.Animation;
using System.Threading;
using System.Media;
using System.IO;
using System.Reflection;
using System.Resources;

namespace WpfGame
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Level1.IsChecked = true;

            Prepare();
           
            canvas1.Focus();

            
        }

        int monstersImgCount = 0;
        int swordsImgCount = 0;

        bool newPlayer = true;

        SoundPlayer soundPlayer = null;

        int cellerSize = 40;
        int maxLevel = 0;
        int sqMaxHeight;
        int sqMaxWidth;

        int gameLevel = 0;

        List<HeroImage> enemiesImages;       
        List<Hero> eggsCondition;

        Dictionary<Point, Hero> map;

        int enemyCount = 1; //с какого номера начинается порядковый номер врагов
        int eggCount = 1;
        int swordCount = 1;

        const int maxHP = 100;
        Hero thiIsHero;
       
        private int _hp;
        private int _heroLevel;
        private double _gamePoints;
     
        public int HeroLevel
        {
            get
            {
                return _heroLevel;
            }

            set
            {
                int delta = value - _heroLevel;

                _heroLevel = value;
                thiIsHero.level = _heroLevel;
                //redraw
                if (map != null && map.Count > 0)
                {
                    var HeroPoint = (from m in map
                                     where m.Value.type == HeroType.Hero
                                     select new { m.Key, m.Value }).Max();

                    Point p = HeroPoint.Key;
                    DeleteImageByName("Hero", true);
                    //DrawChangeLevel(p, thiIsHero, delta);
                    DrawText(p, _heroLevel.ToString(), Colors.Black, "Hero");
                }
                
                labelLevel.Content = _heroLevel + $" / " + maxLevel;
            }
        }

        public double GamePoints
        {
            get
            {
                return _gamePoints;
            }

            set
            {
                _gamePoints = value;
                labelPoint.Content = _gamePoints;
            }
        }

        public int HP
        {
            get
            {
                return _hp;
            }

            set
            {
                int delta = value - _hp;

                if (delta < 0)
                {
                    PlaySound(SoundType.Pain);
                }

                ChangeHP_onForm(delta);

                _hp = value;
                if (_hp > maxHP)
                    _hp = maxHP; 
                
                if (_hp <= 0)
                {
                    CreateAnimationTitle("Вы проиграли!", Colors.Blue);
                    PlaySound(SoundType.Lose);
                    BlockCanvas();
                }          
            }
        }

        public int MonstersImgCount
        {
            get
            {
                return monstersImgCount;
            }

            set
            {
                monstersImgCount = value;
                //lbl_monCount.Content = monstersImgCount;
            }
        }

        public int SwordsImgCount
        {
            get
            {
                return swordsImgCount;
            }

            set
            {
                swordsImgCount = value;
                //lbl_swordCount.Content = swordsImgCount;
            }
        }

        private void Prepare()
        {
            int width = 15, height = 10;
            int minCountTree = 10, maxCountTree = 15;
            int minCountEnemies = 2, maxCountEnemies = 5;
            
            if ((bool)Level1.IsChecked)
            {
                gameLevel = 1;

                width = 15;
                height = 10;
                maxLevel = 20;
      
                minCountTree = 13; maxCountTree = 18;
                minCountEnemies = 2; maxCountEnemies = 5;
            }
            else if ((bool)Level2.IsChecked)
            {
                gameLevel = 2;

                width = 18;
                height = 13;
                maxLevel = 30;

                minCountTree = 23; maxCountTree = 28;
                minCountEnemies = 5; maxCountEnemies = 8;
            }

            canvas1.Height = height * cellerSize;
            canvas1.Width = width * cellerSize;

            window1.Width = canvas1.Width + 200;
            canvasColumn.Width = new GridLength(canvas1.Width);
            window1.Height = canvas1.Height + 150;

            sqMaxHeight = height; sqMaxWidth = width;

            thiIsHero = new Hero() { level = HeroLevel, type = HeroType.Hero };
            HeroLevel = 1;
            HP = maxHP;
            GamePoints = 0;

            Random r = new Random();

            //trees

            int countTrees = r.Next(minCountTree, maxCountTree);
            Point[] pointsTrees = new Point[countTrees];
            for (int i = 0; i < countTrees; i++)
            {
                do
                {
                    pointsTrees[i].X = r.Next(sqMaxWidth);
                    pointsTrees[i].Y = r.Next(sqMaxHeight);
                }
                while (FindElement(pointsTrees, i - 1, pointsTrees[i]));
                //пока в массиве можно найти такой элемент, продолжаем цикл (ищем новый элемент)
            }
            
            //enemies            

            int countEnemies = r.Next(minCountEnemies, maxCountEnemies);
            enemiesImages = new List<HeroImage>(countEnemies);
            eggsCondition = new List<Hero>();

            //maxLevel = sqMaxHeight + sqMaxWidth;

            Point[] pointsEnemies = new Point[countEnemies];
  
            for (int i = 0; i < countEnemies; i++)
            {
                do
                {
                    pointsEnemies[i].X = r.Next(sqMaxWidth);
                    pointsEnemies[i].Y = r.Next(sqMaxHeight);
                }
                while (FindElement(pointsEnemies, i - 1, pointsEnemies[i])
                      || FindElement(pointsTrees, countTrees, pointsEnemies[i]));
            }

            //polozhenie geroya            

            Point heroXY = new Point();

            do
            {
                heroXY.X = r.Next(sqMaxWidth);
                heroXY.Y = r.Next(sqMaxHeight);
            }
            while (FindElement(pointsEnemies, countEnemies, heroXY)
                  || FindElement(pointsTrees, countTrees, heroXY));

            //MAP

            map = new Dictionary<Point, Hero>();

            for (int x = 0; x < sqMaxWidth; x++)
            {
                for (int y = 0; y < sqMaxHeight; y++)
                {
                    Point p = new Point(x, y);
                    Hero h;
                    if (x == heroXY.X && y == heroXY.Y)
                    {
                        h = thiIsHero;
                    }

                    else if (FindElement(pointsEnemies, countEnemies, p))
                    {
                        h = new Hero() { type = HeroType.Enemy, level = SetEnemyLevel(r, HeroLevel, gameLevel) };//maxLevel
                    }

                    else if (FindElement(pointsTrees, countTrees, p))
                    {
                        h = new Hero() { level = 0, type = HeroType.Tree }; 
                    }
                   
                    else
                    {
                        h = new Hero() { level = 0, type = HeroType.Empty };
                    }

                    map.Add(p, h);
                }
            }

            //otrisovka
            DrawMap();

            if ((bool)TurnOnSnow.IsChecked)
            {
                DrawSnow();
            }
        }

        private void DrawSnow()
        {
            canvas1.ClipToBounds = true;

            List<Image> imageList = new List<Image>();

            int snowPictureSize = 170;
        //  int snowPictureDelta = 150;
           
            for (int j = -snowPictureSize; j < canvas1.Height; j += snowPictureSize)
            {
                for (int i = -snowPictureSize; i < canvas1.Width; i += snowPictureSize)
                {
                    Image snow = new Image();
                    snow.Source = new BitmapImage(new Uri(@"Images\Snow.png", UriKind.Relative));
                    snow.Width = snowPictureSize;
                    snow.Name = "snow";
                    
                    Panel.SetZIndex(snow, 5);
                    Canvas.SetTop(snow, j);
                    Canvas.SetLeft(snow, i);
                    canvas1.Children.Add(snow);
                    imageList.Add(snow);
                }
            }
  
            ThicknessAnimation ta = new ThicknessAnimation();
            ta.From = new Thickness(0, 0, 0, 0);
            ta.By = new Thickness(snowPictureSize, snowPictureSize, 0, 0);
            ta.Duration = new Duration(TimeSpan.FromSeconds(5.0));
            ta.RepeatBehavior = RepeatBehavior.Forever;
 
            foreach (Image img in imageList)
            {
                img.BeginAnimation(Image.MarginProperty, ta);
            }
        }

        private void UndrawSnow()
        {
            DeleteImageByName("snow");
        }

        private bool FindElement(Point[] array, int length, Point P)
        {
            for (int i = 0; i < length; i++)
            {
                if (array[i].X == P.X && array[i].Y == P.Y)
                {
                    return true;
                }
            }            
            return false;
        }

        private bool FindElement(Dictionary<Point, Hero> dict, Point XY)
        {
            var elements = (from d in dict
                          where d.Key == XY
                          select d);
            foreach (var el in elements)
            {
                if (el.Value.type == HeroType.Empty)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void ChangeHP_onForm(int countHP)
        {            
            double delta = 0;

            double count = (int)(-countHP * rectHP_base.Width / maxHP);

            if (rectHP.Width + count > rectHP_base.Width)
            {
                delta = rectHP_base.Width - rectHP.Width;
            }
            else if (rectHP.Width + count < 0)
            {
                delta = -rectHP.Width;
            }
            else
            {
                delta = count;
            }

            if ((rectHP.Width + delta <= rectHP_base.Width) && (rectHP.Width + delta >= 0))
            {
                rectHP.Width += delta;
                rectHP.Margin = new Thickness(rectHP.Margin.Left - delta, rectHP.Margin.Top, rectHP.Margin.Right, rectHP.Margin.Bottom);

                //CreateWindowAnimation(delta);
            }
        }

        private void CreateWindowAnimation(double delta)
        {
            if (delta == 0) return;

            ColorAnimation ca = new ColorAnimation();
            ca.From = (Color)ColorConverter.ConvertFromString("#FFCBE2DC");
            if (delta > 0)
            {
               ca.To = Colors.Red;
               //ca.To = (Color)ColorConverter.ConvertFromString("#FFD84545");
            }
            else
            {
                ca.To = Colors.Green;
                //ca.To = (Color)ColorConverter.ConvertFromString("#FF45D845");
            }
            ca.Duration = new Duration(TimeSpan.FromSeconds(0.4));
            ca.AutoReverse = true;
            SolidColorBrush sBrush = new SolidColorBrush(Colors.White);
            sBrush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            
            window1.BorderBrush = sBrush;            
        }

        private void DrawLine(double p1, char way)
        {
            Line line = new Line();

            if (way == 'v')
            {
                line.X1 = p1;
                line.Y1 = 0;
                line.X2 = p1;
                line.Y2 = canvas1.Height;
            }
            else if (way == 'h')
            {
                line.X1 = 0;
                line.Y1 = p1;
                line.X2 = canvas1.Width;
                line.Y2 = p1;
            }

            line.Stroke = new SolidColorBrush(Colors.Gray);
            line.StrokeThickness = 1;
            canvas1.Children.Add(line);
        }

        private void DrawMap()
        {
            for (double i = 0; i <= canvas1.Width; i += cellerSize)
            {
                DrawLine(i, 'v');
            }

            for (double i = 0; i <= canvas1.Height; i += cellerSize)
            {
                DrawLine(i, 'h');
            }            

            foreach (KeyValuePair<Point, Hero> i in map)
            {
                if (i.Value.type == HeroType.Hero)
                {
                    DrawHero(i.Key, i.Value);
                }
                else if (i.Value.type == HeroType.Enemy)
                {
                    DrawEnemy(i.Key, i.Value, "Enemy" + enemyCount++);
                }
                else if (i.Value.type == HeroType.Tree)
                {
                    DrawTree(i.Key, i.Value);
                }
            }       
        }

        private void DrawHero(Point p, Hero h, Direction direction = Direction.Down)
        {            
            Image imgHero = new Image();
            string imgAddress = @"Images\002-Fighter02";

            if (newPlayer)
            {
                imgAddress = @"Images\hero";
            }
            
            if (direction == Direction.Down)
            {
                imgAddress += "_f";
            }
            else if (direction == Direction.Up)
            {
                imgAddress += "_b";
            }
            else if (direction == Direction.Left)
            {
                imgAddress += "_l";
            }
            else if (direction == Direction.Right)
            {
                imgAddress += "_r";
            }

            imgAddress += ".png";
            imgHero.Source = new BitmapImage(new Uri(imgAddress, UriKind.Relative));

            imgHero.Height = cellerSize;
            Canvas.SetTop(imgHero, p.Y * cellerSize);
            Canvas.SetLeft(imgHero, p.X * cellerSize);
            canvas1.Children.Add(imgHero);

            imgHero.Name = "Hero";
            DrawText(p, h.level.ToString(), Colors.Black, "Hero");
        }

        private void DrawEnemy(Point p, Hero enemy, string enemyNumber, Direction direction = Direction.Down)
        {            
            Image enemyImg = new Image();

            string imgAddress = @"Images\Monster";
            if (direction == Direction.Down)
            {
                imgAddress += "_f";
            }
            else if (direction == Direction.Up)
            {
                imgAddress += "_b";
            }
            else if (direction == Direction.Left)
            {
                imgAddress += "_l";
            }
            else if (direction == Direction.Right)
            {
                imgAddress += "_r";
            }

            imgAddress += ".png";
            enemyImg.Source = new BitmapImage(new Uri(imgAddress, UriKind.Relative));

            enemyImg.Height = cellerSize;
            Canvas.SetTop(enemyImg, p.Y * cellerSize);
            Canvas.SetLeft(enemyImg, p.X * cellerSize);
            canvas1.Children.Add(enemyImg);

            enemyImg.Name = enemyNumber;
            DrawText(p, enemy.level.ToString(), Colors.Black, enemyNumber);

            HeroImage hImg = new HeroImage() { hero = enemy, heroImgText = enemyImg.Name };
            enemiesImages.Add(hImg);
        }

        private void DrawEgg(Point p, Hero egg, string eggNumber)
        {            
            Image enemyImg = new Image();
            enemyImg.Source = new BitmapImage(new Uri(@"Images\egg1.png", UriKind.Relative));
            enemyImg.Height = cellerSize;
            Canvas.SetTop(enemyImg, p.Y * cellerSize);
            Canvas.SetLeft(enemyImg, p.X * cellerSize);
            canvas1.Children.Add(enemyImg);

            enemyImg.Name = eggNumber;
            DrawText(p, egg.level.ToString(), Colors.Red, enemyImg.Name);

            HeroImage hImg = new HeroImage() { hero = egg, heroImgText = eggNumber };
            enemiesImages.Add(hImg);            
        }

        private void DrawTree(Point p, Hero h)
        {
            Image tree = new Image();
            tree.Source = new BitmapImage(new Uri(@"Images\tree1.png", UriKind.Relative));
            tree.Width = cellerSize;
            Canvas.SetTop(tree, p.Y * cellerSize);
            Canvas.SetLeft(tree, p.X * cellerSize);
            canvas1.Children.Add(tree);

            tree.Name = "tree";
        }

        private void DrawFruit(Point p, Hero h)
        {
            Image fruit = new Image();
            fruit.Source = new BitmapImage(new Uri(@"Images\fruit1.png", UriKind.Relative));
            fruit.Width = cellerSize;
            Canvas.SetTop(fruit, p.Y * cellerSize);
            Canvas.SetLeft(fruit, p.X * cellerSize);
            canvas1.Children.Add(fruit);

            HeroImage hImg = new HeroImage() { hero = h, heroImgText = "fruit" };
            enemiesImages.Add(hImg);
            fruit.Name = "fruit";
        }

        private void DrawFire(Point p, Direction direction)
        {
            Image fire = new Image();
            fire.Source = new BitmapImage(new Uri(@"Images\Fire1.png", UriKind.Relative));
            fire.Width = cellerSize;
            Canvas.SetTop(fire, p.Y * cellerSize);
            Canvas.SetLeft(fire, (p.X + 1) * cellerSize);
            canvas1.Children.Add(fire);            
        }

        private void DrawSword(Point p, Hero h)
        {
            Image sword = new Image();
            sword.Source = new BitmapImage(new Uri(@"Images\sword.png", UriKind.Relative));
            sword.Width = cellerSize;
            sword.Name = "sword" + swordCount++;

            Panel.SetZIndex(sword, 1);
            Canvas.SetTop(sword, p.Y * cellerSize);
            Canvas.SetLeft(sword, p.X * cellerSize);
            canvas1.Children.Add(sword);

            PlaySound(SoundType.Sword);

            DoubleAnimation da = new DoubleAnimation();
            da.From = 0.0;
            da.To = 1.0;
            da.Duration = new Duration(TimeSpan.FromSeconds(0.15));
            da.AutoReverse = true;
            da.Completed += delegate { UndrawEnemy_Completed(h, sword.Name); };
            sword.BeginAnimation(Image.OpacityProperty, da);

            //SwordsImgCount++;
            //

            string strImage = "";
            foreach (HeroImage eImg in enemiesImages)
            {
                if (h == eImg.hero)
                {
                    strImage = eImg.heroImgText;
                    break;
                }
            }
            List<UIElement> imagesList = FindImageByName(strImage);
            DoubleAnimation da_img = new DoubleAnimation();
            da_img.From = 1.0;
            da_img.To = 0.2;
            da_img.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            foreach (UIElement ui in imagesList)
            {
                ui.BeginAnimation(OpacityProperty, da_img);
            }
        }

        private void UndrawEnemy_Completed(Hero h, string swordName)
        {
            //MonstersCount++;
            DeleteImageEnemy(h);
            DeleteImageByName(swordName); 
            //"sword"           
        }

        private void DrawChangeLevel(Point p, Hero h, int delta)
        {
            string deltaTxt = delta.ToString();
            Color color = Colors.Red;
            if (delta > 0)
            {
                color = Colors.Green;
                deltaTxt = "+" + deltaTxt;
            }

            TextBlock tBlock = Text(p.X * cellerSize + (cellerSize * 3 / 4), p.Y * cellerSize + 1, deltaTxt, color, new Color(), 12, "level");
            Canvas.SetZIndex(tBlock, 1);

            TimeSpan ts = TimeSpan.FromSeconds(0.5);

            ThicknessAnimation ta = new ThicknessAnimation();
            //if (delta > 0)
            //{
            //    ta.From = new Thickness(0, -50, 0, 0);
            //    ta.By = new Thickness(0, 0, 0, 0);
            //}
            //else if (delta < 0)
            //{
                ta.From = new Thickness(0, 0, 0, 0);
                ta.By = new Thickness(0, -10, 0, 0);
            //}
            ta.Duration = new Duration(ts);

            DoubleAnimation da = new DoubleAnimation();
            da.From = 1.0;
            da.To = 0.0;
            ta.Duration = new Duration(ts);

            tBlock.BeginAnimation(TextBlock.MarginProperty, ta);
            tBlock.BeginAnimation(TextBlock.OpacityProperty, da);
        }

        private void DrawText(Point p, string text, Color color, string Name = "")
        {
            Text(p.X * cellerSize + (cellerSize * 3 / 4), p.Y * cellerSize + 1, text, color, Colors.White, 12, Name);
        }
        
        private TextBlock Text(double x, double y, string text, Color color, Color backColor, double FontSize, string Name)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = FontSize;  //12
            textBlock.Background = new SolidColorBrush(backColor);  //new SolidColorBrush(Colors.White);
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            int textNumber = canvas1.Children.Add(textBlock);
            textBlock.Name = Name;
            return textBlock;
        }

        private void PlaySound(SoundType sType)
        {
            if (!(bool)TurnOnSound.IsChecked)
            {
                return;
            }

            Stream soundStream = null;
            if (sType == SoundType.Sword)
            {
                soundStream = Properties.Resources.Attack1;
            }
            else if (sType == SoundType.Fruit)
            {
                soundStream = Properties.Resources.Heal1;
            }
            else if (sType == SoundType.Pain)
            {
                soundStream = Properties.Resources.Pain1;
            }
            else if (sType == SoundType.Win)
            {
                soundStream = Properties.Resources.Win;
            }
            else if (sType == SoundType.Lose)
            {
                soundStream = Properties.Resources.Lose;
            }

            if (soundStream == null)
            {
                return;
            }

            soundPlayer = new SoundPlayer(soundStream);
            soundPlayer.Play();

        }

        private void canvas1_KeyDown(object sender, KeyEventArgs e)
        {
            //canvas1.Focus();
            if ((e.Key == Key.Right) || (e.Key == Key.Left) || (e.Key == Key.Up) || (e.Key == Key.Down))
            {
                HeroMove(e.Key);

                //if (makeAMove)
                //{
                //    //2018_08_22 Смена_динамики
                //    //++
                //    map[HeroPoint.Key] = new Hero() { level = 0, type = HeroType.Empty };
                //    map[p] = thiIsHero;
                //    //--

                //    DeleteImageByName("Hero");
                //    DrawHero(p, thiIsHero, direction);
                //}
                ////2018_08_22 Смена_динамики
                ////++
                //else
                //{
                //    map[p] = new Hero() { level = 0, type = HeroType.Empty };
                //}
                ////--

                //// враг двигается к герою
                //MoveEnemyToHero(p);

                //подкрепить силы!
                int fruitCount = (from m in map
                                  where m.Value.type == HeroType.Fruit
                                  select m).Count();
                if (fruitCount == 0)
                {
                    CreateFruit();
                }

                //WIN
                //canvas1.IsEnabled = false;

                int enemyCount = (from m in map
                                  where (m.Value.type == HeroType.Enemy || m.Value.type == HeroType.Egg)
                                  select m).Count();

                if (enemyCount == 0)
                {
                    //MessageBox.Show("Вы выиграли!");    //You win
                    CreateAnimationTitle("Вы выиграли!", Colors.Red);
                    PlaySound(SoundType.Win);
                    BlockCanvas();
                }

            }

            //Shift
            
            canvas1.Focus();
        }

        private void HeroMove(Key eKey)
        {
            var HeroPoints = (from m in map
                              where m.Value == thiIsHero
                              select new { m.Key, m.Value });

            if (HeroPoints == null)
                return;

            var HeroPoint = HeroPoints.Max();
            //where m.Value.type == HeroType.Hero

            //Hero hero = HeroPoint.Value;
            Point p = HeroPoint.Key;

            Direction direction = Direction.Down;

            if (eKey == Key.Right)
            {
                p = new Point(HeroPoint.Key.X + 1, HeroPoint.Key.Y);
                direction = Direction.Right;
            }
            else if (eKey == Key.Left)
            {
                p = new Point(HeroPoint.Key.X - 1, HeroPoint.Key.Y);
                direction = Direction.Left;
            }
            else if (eKey == Key.Up)
            {
                p = new Point(HeroPoint.Key.X, HeroPoint.Key.Y - 1);
                direction = Direction.Up;
            }
            else if (eKey == Key.Down)
            {
                p = new Point(HeroPoint.Key.X, HeroPoint.Key.Y + 1);
                direction = Direction.Down;
            }

            //просто разворот                
            DeleteImageByName("Hero");
            DrawHero(HeroPoint.Key, thiIsHero, direction);

            //проверка, что в эту точку можно переместиться
            if (!map.Keys.Contains(p))
            {
                return;
            }

            //bool makeAMove = true;
            bool dontMakeAMove = false;
            if (map[p].type != HeroType.Empty)
            {
                if (map[p].type == HeroType.Fruit)
                {
                    HP += 50;

                    PlaySound(SoundType.Fruit);
                    //удаляем
                    DeleteImageEnemy(p);
                    //герой встанет на это место дальше
                }

                else if (map[p].type != HeroType.Enemy) //это елка
                {
                    return;
                }

                else
                {
                    //проверка, что можно пройти этого врага
                    if (map[p].level <= HeroLevel) //<=
                    {
                        //повышаем уровень
                        if (map[p].level == HeroLevel)
                        {
                            HeroLevel++;
                        }

                        AddGamePoints(map[p]);
                        DrawSword(p, map[p]);
                        //DeleteImageEnemy(p);
                        dontMakeAMove = true;

                        //AddGamePoints(map[p]);                        
                        //DrawSword(p, map[p], true, direction);                           
                        //makeAMove = false;
                        ////DeleteImageEnemy(p);
                    }
                    else
                    {
                        //уменьшаем жизнь
                        HeroAndEnemyInTheSamePoint(p);
                        //менем состояния яиц
                        ChangeEggsCondition();

                        return;
                    }
                }
            }

            map[HeroPoint.Key] = new Hero() { level = 0, type = HeroType.Empty };
            map[p] = thiIsHero;

            DeleteImageByName("Hero");
            DrawHero(p, thiIsHero, direction);

            // враг двигается к герою
            MoveEnemyToHero(p);
        }

        private void BlockCanvas()
        {
            canvas1.IsEnabled = false;
        }

        private void CreateFruit()
        {
            if (HP <= 0.7 * maxHP)
            {
                Random r = new Random();
                if (r.Next(5) == 0)
                {
                    //create a fruit
                 
                    Point p = new Point(-1, -1);
                    do
                    {
                        p = new Point(r.Next(sqMaxWidth), r.Next(sqMaxHeight));
                    }
                    while (map[p].type != HeroType.Empty);

                    map[p] = new Hero() { type = HeroType.Fruit, level = 0 };
                    DrawFruit(p, map[p]);
                }
            }
        }

        /// <summary>
        /// процедура удаляет изображение врага, находящееся в данной точке
        /// если он содержится в списке enemiesImages
        /// применяется для Enemies, Eggs
        /// </summary>
        /// <param name="p">Точка, в которой находится враг</param>
        /// <param name="onlyText">удалять только текст - применяется для Eggs (обратный отсчет)</param>
        private void DeleteImageEnemy(Point p, bool onlyText = false)
        {
            bool f = false;
            //ищем в List enemies                            
            foreach (HeroImage hImg in enemiesImages)
            {
                if (hImg.hero == map[p])
                {
                    //удаляем изображение врага
                    DeleteImageByName(hImg.heroImgText, onlyText);
                    //с карты враг удаляется далее, когда герой заходит в клетку
                    f = true;
                    break;
                }
            }

            if (!onlyText)
            {
                enemiesImages.RemoveAll(n => n.hero == map[p]);
            }

            if (!f)
            {
                labelPoint.Content += "!";
            }
        }

        /// <summary>
        /// процедура удаляет изображение врага, находящееся в данной точке
        /// если он содержится в списке enemiesImages
        /// применяется для Enemies, Eggs
        /// </summary>
        /// <param name="h">Враг</param>
        /// <param name="onlyText">удалять только текст - применяется для Eggs (обратный отсчет)</param>
        private void DeleteImageEnemy(Hero h, bool onlyText = false)
        {
            bool f = false;
            //ищем в List enemies                            
            foreach (HeroImage hImg in enemiesImages)
            {
                if (hImg.hero == h)
                {
                    //удаляем изображение врага
                    DeleteImageByName(hImg.heroImgText, onlyText);
                    //с карты враг удаляется далее, когда герой заходит в клетку
                    f = true;
                    break;
                }
            }

            if (!onlyText)
            {
                enemiesImages.RemoveAll(n => n.hero == h);
            }

            if (!f)
            {
                labelPoint.Content += "!";
            }
        }

        /// <summary>
        /// процедура удаляет изображение и подпись по имени
        /// поиск производится по всей canvas1
        /// также можно удалить только текст
        /// </summary>
        private void DeleteImageByName(string imgHeroName, bool onlyText = false)
        {
            //будем искать саму картинку по имени (задается при отрисовке)
            
            UIElement heroImg = null;
            UIElement heroTxt = null;

            List<UIElement> uiList = new List<UIElement>();

            foreach (UIElement ui in canvas1.Children)
            {
                if (!onlyText && ui.GetType() == typeof(Image))
                {
                    Image i = (Image)ui;                
                    if (i.Name == imgHeroName)
                    {
                        heroImg = ui;
                        //
                        //canvas1.Children.Remove(heroImg);
                        uiList.Add(heroImg);
                    }
                }
                if (ui.GetType() == typeof(TextBlock))
                {
                    TextBlock i = (TextBlock)ui;
                    if (i.Name == imgHeroName)
                    {
                        heroTxt = ui;
                        //
                        //canvas1.Children.Remove(heroTxt);
                        uiList.Add(heroTxt);
                    }
                }                
            }                   
            
            foreach (UIElement ui in uiList)
            {
                canvas1.Children.Remove(ui);
            }      
        }

        private List<UIElement> FindImageByName(string imgHeroName, bool onlyText = false)
        {
            //будем искать саму картинку по имени (задается при отрисовке)

            UIElement heroImg = null;
            UIElement heroTxt = null;

            List<UIElement> uiList = new List<UIElement>();

            foreach (UIElement ui in canvas1.Children)
            {
                if (!onlyText && ui.GetType() == typeof(Image))
                {
                    Image i = (Image)ui;
                    if (i.Name == imgHeroName)
                    {
                        heroImg = ui;
               
                        uiList.Add(heroImg);
                    }
                }
                if (ui.GetType() == typeof(TextBlock))
                {
                    TextBlock i = (TextBlock)ui;
                    if (i.Name == imgHeroName)
                    {
                        heroTxt = ui;
          
                        uiList.Add(heroTxt);
                    }
                }
            }

            return uiList;
        }

        //процедура двигает ближайшего врага к герою после хода героя
        //местоположение героя задается параметром
        private void MoveEnemyToHero(Point heroPoint)
        {
            int step = 1;
            Vector v;

            var enemyPoints = from m in map
                              where m.Value.type == HeroType.Enemy
                              orderby Point.Subtract(heroPoint, m.Key).Length
                              select m.Key;

            foreach (var thisPoint in enemyPoints)
            {
                v = Point.Subtract(heroPoint, thisPoint);

                //проверить, что нет препятствий
                if (Math.Abs(v.X) >= Math.Abs(v.Y))
                {
                    int newStep = (v.X > 0) ? step : -step;
                    Point newPoint = new Point(thisPoint.X + newStep, thisPoint.Y);
                    if (map[newPoint].type == HeroType.Empty)
                    {
                        //its OK

                        //в новую точку ставим героя, в старую - пустое место
                        //а что происходит в map? удаляется ли оттуда старая точка?
                        //ответ: в map кол-во эл-в то же
                        MoveEnemy(thisPoint, newPoint, heroPoint);

                        //подвинули одного и вышли из цикла
                        break;
                    }
                    else if (map[newPoint].type == HeroType.Hero)
                    {
                        //герой и враг столкнулись
                        //thisPoint - т.к. еще до шага врага, он пока на прежнем месте
                        HeroAndEnemyInTheSamePoint(thisPoint);
                        break;
                    }
                    else
                    {
                        //иначе пробуем двинуть по У
                        //только если Y != 0
                        if (v.Y == 0)
                        {
                            continue;
                        }

                        newStep = (v.Y > 0) ? step : -step;
                        newPoint = new Point(thisPoint.X, thisPoint.Y + newStep);
                        if (map[newPoint].type == HeroType.Empty)
                        {
                            MoveEnemy(thisPoint, newPoint, heroPoint);
                            break;
                        }
                        else if (map[newPoint].type == HeroType.Hero)
                        {
                            HeroAndEnemyInTheSamePoint(thisPoint);
                            break;
                        }
                        else
                        //иначе ищем след. точку
                        {
                            continue;
                        }
                    }
                }
                else
                //Y > X
                {
                    int newStep = (v.Y > 0) ? step : -step;
                    Point newPoint = new Point(thisPoint.X, thisPoint.Y + newStep);
                    if (map[newPoint].type == HeroType.Empty)
                    {
                        MoveEnemy(thisPoint, newPoint, heroPoint);
                        break;
                    }
                    else if (map[newPoint].type == HeroType.Hero)
                    {
                        HeroAndEnemyInTheSamePoint(thisPoint);
                        break;
                    }
                    else
                    {
                        //только если X != 0
                        if (v.X == 0)
                        {
                            continue;
                        }
                        newStep = (v.X > 0) ? step : -step;
                        newPoint = new Point(thisPoint.X + newStep, thisPoint.Y);
                        if (map[newPoint].type == HeroType.Empty)
                        {
                            MoveEnemy(thisPoint, newPoint, heroPoint);
                            break;
                        }
                        else if (map[newPoint].type == HeroType.Hero)
                        {
                            HeroAndEnemyInTheSamePoint(thisPoint);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            //массив яиц, обработка

            ChangeEggsCondition();
        }

        private void ChangeEggsCondition()
        {
            foreach (Hero egg in eggsCondition)
            {
                egg.level--;

                //ищем точку, удаляем яйцо
                var points = (from m in map
                              where m.Value == egg
                              select m.Key);
                if (points == null)
                    break;

                var point = points.Max();

                if (egg.level == 0)
                {
                    if (point != null)
                    {
                        DeleteImageEnemy(point);
                    }
                    //добавление нового врага
                    Random r = new Random();
                    Hero newEnemy = new Hero() { type = HeroType.Enemy, level = SetEnemyLevel(r, HeroLevel, gameLevel) };//maxLevel
                    Point ePoint = point;
                    map[ePoint] = newEnemy;
                    DrawEnemy(ePoint, newEnemy, "Enemy" + enemyCount++);
                }
                else
                {
                    //перерисовываем уровень
                    if (point != null)
                    {
                        DeleteImageEnemy(point, true); //true
                    }
                    //DrawEgg(point, egg, "Egg" + eggCount++);

                    //ищем этот egg в списке enemiesImages
                    HeroImage thisHI = enemiesImages.Find(n => n.hero == egg);
                    if (thisHI != null)
                    {
                        DrawText(point, egg.level.ToString(), Colors.Red, thisHI.heroImgText);
                    }
                }
            }

            eggsCondition.RemoveAll(n => n.level == 0);
        }

        private int SetEnemyLevel(Random r, int heroLevel, int gameLevel)
        {
            int ML = maxLevel - 1;
            int delta = 5;

            if (gameLevel == 1)
            {
                delta = 5;
            }
            else if (gameLevel == 2)
            {
                delta = 6;
            }

            int maxLevelEnemy = heroLevel + delta <= ML ? heroLevel + delta : ML;
            int minLevelEnemy = heroLevel <= ML ? heroLevel : ML;
            return r.Next(minLevelEnemy, maxLevelEnemy);
        }

        //действия, если герой и враг оказались в одной точке
        private void HeroAndEnemyInTheSamePoint(Point thisPoint)
        {
            //в зависимости от уровня герой получает урон ИЛИ убивает врага И/ИЛИ повышает уровень
            //удаляем врага, повышаем уровень
            Hero enemy = map[thisPoint];

            if (enemy.level <= HeroLevel)
            {
                //DrawSword(thisPoint, enemy, false);                
                //DeleteImageEnemy(thisPoint);

                DrawSword(thisPoint, enemy);
                //System.Threading.Thread.Sleep(1000);
                //DeleteImageEnemy(thisPoint);

                if (enemy.level == HeroLevel)
                    HeroLevel++;

                map[thisPoint] = new Hero() { level = 0, type = HeroType.Empty };

                //начисляем очки

                AddGamePoints(enemy);
            }
            //получаем урон
            else
            {
                int substractLevel = map[thisPoint].level - HeroLevel;
                if (substractLevel <= 5)
                {
                    HP -= 20;
                }
                else if (substractLevel <= 15)
                {
                    HP -= 30;
                }
                else
                {
                    HP -= 50;
                }

                //наносим урон врагу
                //map[thisPoint].level--;
                //DrawChangeLevel(thisPoint, map[thisPoint], -1);
            }
        }

        private void AddGamePoints(Hero enemy)
        {
            GamePoints += enemy.level * 10;
        }

        //процедура перемещает врага из одной точки в другую
        //что это за враг, понятно по его начальному местоположению
        private void MoveEnemy(Point thisPoint, Point newPoint, Point heroPoint)
        {
            Hero enemy = map[thisPoint];
            map[newPoint] = enemy;
            map[thisPoint] = new Hero() { level = 0, type = HeroType.Empty };

            string enemyText = "";
            //ищем в List enemies   
            //не через проц. тк присваиваем имя                         
            foreach (HeroImage hImg in enemiesImages)
            {
                if (hImg.hero == enemy)
                {
                    //удаляем врага с canvas
                    DeleteImageByName(hImg.heroImgText);
                    //запоминаем его имя
                    enemyText = hImg.heroImgText;
                    break;
                }
            }

            //удаляем имя из списка, чтобы в след. проц. добавить вновь???
            enemiesImages.RemoveAll(n => n.hero == enemy);

            Vector vEnemy = new Vector();
            vEnemy = Point.Subtract(newPoint, thisPoint);
            Direction direct = Direction.Down;
            if (vEnemy.X > 0)
            {
                direct = Direction.Right;
            }
            else if (vEnemy.X < 0)
            {
                direct = Direction.Left;
            }
            else if (vEnemy.Y > 0)
            {
                direct = Direction.Down;
            }
            else if (vEnemy.Y < 0)
            {
                direct = Direction.Up;
            }

            //относительно героя. если на одной линии - развернуть к герою
            vEnemy = Point.Subtract(heroPoint, newPoint);
            if (vEnemy.X > 0 && vEnemy.Y == 0)
            {
                direct = Direction.Right;
            }
            else if (vEnemy.X < 0 && vEnemy.Y == 0)
            {
                direct = Direction.Left;
            }
            else if (vEnemy.Y > 0 && vEnemy.X == 0)
            {
                direct = Direction.Down;
            }
            else if (vEnemy.Y < 0 && vEnemy.X == 0)
            {
                direct = Direction.Up;
            }

            DrawEnemy(newPoint, enemy, enemyText, direct);

            //оставляем яйцо

            Random r = new Random();
            int chance = r.Next(5); //шанс 1 к 5
            if (chance == 0)
            {
                //Egg. Создаем, добавляем в массив состояний, добавляем на map, отрисовываем
                Hero egg = new Hero { level = 5, type = HeroType.Egg };
                eggsCondition.Add(egg);
                map[thisPoint] = egg;
                DrawEgg(thisPoint, egg, "Egg" + eggCount++);
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void StartNewGame()
        {
            canvas1.Children.Clear();
            map.Clear();
            enemiesImages.Clear();
            eggsCondition.Clear();
            if (soundPlayer != null)
                soundPlayer.Stop();
            enemyCount = 0;
            eggCount = 0;

            MonstersImgCount = 0;
            SwordsImgCount = 0;

            Prepare();
            canvas1.IsEnabled = true;
            canvas1.Focus();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string Help = "Управление: ↑ ↓ ← →" + Environment.NewLine;
            Help += "Монстр более высокого уровня при столкновении наносит ущерб." + Environment.NewLine;
            Help += "Монстр более низкого или равного уровня при столкновении погибает." + Environment.NewLine;
            Help += Environment.NewLine + "Отзывы и предложения: l.kostuchenko@yandex.ru";
            MessageBox.Show(Help, "Справка");
        }

        private void CreateAnimationTitle(string title, Color color)
        {
            TextBlock titleTxt = Text(1, (sqMaxHeight - 3) * cellerSize * 0.5, title, color, Colors.White, 30, "title111");

            titleTxt.Width = sqMaxWidth * cellerSize - 2;
            titleTxt.TextAlignment = TextAlignment.Center; 
            titleTxt.FontFamily = new FontFamily("Monotype Corsiva");
            titleTxt.FontStretch = FontStretches.UltraExpanded;
            titleTxt.Padding = new Thickness(5, cellerSize, 5, cellerSize);
            Canvas.SetZIndex(titleTxt, 10);
                
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0.25;
            da.To = 0.80;
            da.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            titleTxt.BeginAnimation(TextBlock.OpacityProperty, da);

            ColorAnimation ca = new ColorAnimation();
            ca.From = color;
            ca.To = Colors.Violet;
            ca.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            ca.AutoReverse = true;
            ca.RepeatBehavior = new RepeatBehavior(1);
            SolidColorBrush sBrush = new SolidColorBrush(color);
            sBrush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            titleTxt.Foreground = sBrush;
                        
        }

        private void TurnOnSnow_Checked(object sender, RoutedEventArgs e)
        {
            DrawSnow();
        }

        private void TurnOnSnow_Unchecked(object sender, RoutedEventArgs e)
        {
            UndrawSnow();
        }

        private void NewGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartNewGame();
        }

        private void Help_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Help_Click(sender, e);
        }
        
        private void Level1_Checked(object sender, RoutedEventArgs e)
        {
            if (gameLevel == 0)
            {
                gameLevel = 1;
                return;
            }
                        
            MessageBoxResult res = MessageBox.Show("Начать новую игру?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (res == MessageBoxResult.Yes)
            {
                StartNewGame();
            }
        }

        private void Level2_Checked(object sender, RoutedEventArgs e)
        {
            if (gameLevel == 0)
            {
                gameLevel = 2;
                return;
            }

            MessageBoxResult res = MessageBox.Show("Начать новую игру?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                StartNewGame();
            }
        }



        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        int last_aaa;

        private void btn_Auto_Click(object sender, RoutedEventArgs e)
        {
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Start();
            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            int aaa;
            Random r = new Random();
            do
            {
                aaa = r.Next(0, 4);
            }
            while (last_aaa == aaa);
            
            switch (aaa)
            {
                case 0:
                    HeroMove(Key.Up);
                    break;
                case 1:
                    HeroMove(Key.Right);
                    break;
                case 2:
                    HeroMove(Key.Down);
                    break;
                case 3:
                    HeroMove(Key.Left);
                    break;
            }

            last_aaa = aaa;
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }


    }
}
