using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfGame
{
    class Hero
    {
        public int level { get; set; }
        public HeroType type { get; set; }
    }

    enum Direction { Up, Down, Left, Right}

    enum HeroType { Hero, Enemy, Tree, Empty, Egg, Fruit}

    enum SoundType { Sword, Pain, Fruit, Win, Lose}

    class HeroImage
    {
        public Hero hero { get; set; }
        public string heroImgText { get; set; }    
    }
}
