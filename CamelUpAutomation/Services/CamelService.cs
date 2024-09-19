using CamelUpAutomation.Enums;
using CamelUpAutomation.Models;
using CamelUpAutomation.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelUpAutomation.Services
{

    public interface  ICamelService
    {
        Game MoveCrazyCamels(Game game);
        IEnumerable<Camel> GenerateCamels();
        IEnumerable<Camel> GetCrazyCamelTest1();
        IEnumerable<Camel> GetCrazyCamelTest2();
        IEnumerable<Camel> GetCrazyCamelTest3();
    }
    public class CamelService : ICamelService
    {
        private ICryptoService _cryptoService;

        public CamelService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }
        // the white one should move
        public IEnumerable<Camel> GetCrazyCamelTest1()
        {
            var camels = GenerateCamels();
            var whiteCamel = camels.FirstOrDefault(c => c.Color == CamelColor.White);
            var blueCamel = camels.FirstOrDefault(c => c.Color == CamelColor.Blue);

            whiteCamel.Position = 10;
            whiteCamel.Height = 0;
            blueCamel.Position = 10;
            blueCamel.Height = 1;
            return camels;
        }
        // the black one should move
        public IEnumerable<Camel> GetCrazyCamelTest2()
        {
            var camels = GenerateCamels();
            var whiteCamel = camels.FirstOrDefault(c => c.Color == CamelColor.White);
            var blackCamel = camels.FirstOrDefault(c => c.Color == CamelColor.Black);

            whiteCamel.Position = 10;
            whiteCamel.Height = 0;
            blackCamel.Position = 10;
            blackCamel.Height = 1;
            return camels;
        }
        // either the white or the black one should move
        public IEnumerable<Camel> GetCrazyCamelTest3()
        {
            var camels = GenerateCamels();
            var whiteCamel = camels.FirstOrDefault(c => c.Color == CamelColor.White);
            var blueCamel = camels.FirstOrDefault(c => c.Color == CamelColor.Blue);

            whiteCamel.Position = 10;
            whiteCamel.Height = 0;
            blueCamel.Position = 11;
            blueCamel.Height = 0;
            return camels;
        }

        public Game MoveCrazyCamels(Game game)
        {
            var random = new Random();
            var whiteFirst = random.Next(0, 2) == 0;
            var role1 = random.Next(1, 4);
            var role2 = random.Next(1, 4);
            var whiteCamel = game.Camels.FirstOrDefault(c => c.Color == CamelColor.White);
            var blackCamel = game.Camels.FirstOrDefault(c => c.Color == CamelColor.Black);
            
            var firstCamel = whiteFirst ? whiteCamel : blackCamel;
            var secondCamel = whiteFirst ? blackCamel : whiteCamel;

            firstCamel.Position -= role1;
            secondCamel.Position -= role2;
            if (role1 == role2)
            {
                secondCamel.Height++;
            }

            return game;
        }

        public IEnumerable<Camel> GenerateCamels()
		{
            IList<Camel> camels = new List<Camel>();
            foreach (CamelColor color in Enum.GetValues(typeof(CamelColor)))
			{
				var crazyCamel = (color == CamelColor.White || color == CamelColor.Black);
                Camel camel = new Camel
				{
                    id = _cryptoService.GenerateRandomString(),
                    Color = color,
                    Position = crazyCamel ? 17 : 0,
                    Height = 0,
                    IsCrazyCamel = crazyCamel,
                };
                camels.Add(camel);
            }
            return camels;
        }
    }
}
