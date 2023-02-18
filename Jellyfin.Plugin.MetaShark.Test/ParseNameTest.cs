using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using Jellyfin.Plugin.MetaShark.Providers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class ParseNameTest
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));



        [TestMethod]
        public void TestMovieParse()
        {
            // 混合中英文
            var fileName = "新世界.New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            var parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "V字仇杀队.V.for.Vendetta.2006";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "罗马假日.Roman.Holiday.1953.WEB-DL.1080p.x265.AAC.2Audios.GREENOTEA";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // 只英文
            fileName = "A.Chinese.Odyssey.Part.1.1995.BluRay.1080p.x265.10bit.2Audio-MiniHD";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "Who.Am.I.1998.1080p.BluRay.x264.DTS-FGT";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // 只中文
            fileName = "机动战士高达 逆袭的夏亚";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "秒速5厘米";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());


            // 标题加年份
            fileName = "V字仇杀队 (2006)";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());


            // anime
            fileName = "[SAIO-Raws] もののけ姫 Mononoke Hime [BD 1920x1036 HEVC-10bit OPUSx2 AC3].mp4";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());
        }

        [TestMethod]
        public void TestTVSeriesParse()
        {
            // 混合中英文
            var fileName = "新世界.New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            var parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // 混合中英文带副标题
            fileName = "航海王：狂热行动.One.Piece.Stampede.2019.BD720P.X264.AAC.Japanese&Mandarin.CHS.Mp4Ba";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // 只英文
            fileName = "She-Hulk.Attorney.at.Law.S01.1080p.WEBRip.x265-RARBG";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "Bright.Future.S01.2022.2160p.HDR.WEB-DL.H265.AAC-BlackTV[BTBTT]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "Back.to.the.Future.Part.II.1989.BluRay.1080p.x265.10bit.2Audio-MiniHD[BTBTT]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());



            // anime混合中日文
            fileName = "[异域-11番小队][罗马浴场 THERMAE_ROMAE][1-6+SP][BDRIP][720P][X264-10bit_AAC]";
            var anitomyResult = AnitomySharp.AnitomySharp.Parse(fileName);
            Console.WriteLine(anitomyResult.ToJson());

            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // anime
            fileName = "[Nekomoe kissaten][Shin Ikkitousen][01-03][720p][CHT]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "[SAIO-Raws] Fullmetal Alchemist Brotherhood [BD 1920x1080 HEVC-10bit OPUS][2009]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());
        }

        [TestMethod]
        public void TestEposideParse()
        {
            // 混合中英文
            var fileName = "新世界.New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            var parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // 只英文
            fileName = "She-Hulk.Attorney.At.Law.S01E01.1080p.WEBRip.x265-RARBG";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // anime
            fileName = "[YYDM-11FANS][THERMAE_ROMAE][02][BDRIP][720P][X264-10bit_AAC][7FF2269F]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            // anime带季数
            fileName = "[WMSUB][Detective Conan - Zero‘s Tea Time ][S01][E06][BIG5][1080P].mp4";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

            fileName = "[KTXP][Machikado_Mazoku_S2][01][BIG5][1080p]";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());


            // anime特典
            fileName = "[KissSub][Steins;Gate][SP][GB_BIG5_JP][BDrip][1080P][HEVC] 边界曲面的缺失之环";
            parseResult = NameParser.Parse(fileName);
            Console.WriteLine(parseResult.ToJson());

        }

        [TestMethod]
        public void TestCheckExtra()
        {
            var name = "[VCB-Studio] Spice and Wolf [CM02][Ma10p_1080p][x265_flac]";
            var result = NameParser.IsExtra(name);
            Console.WriteLine(result);

            name = "[VCB-Studio] Spice and Wolf [Menu01_2][Ma10p_1080p][x265_flac]";
            result = NameParser.IsExtra(name);
            Console.WriteLine(result);

            name = "[VCB-Studio] Spice and Wolf [NCED][Ma10p_1080p][x265_flac]";
            result = NameParser.IsExtra(name);
            Console.WriteLine(result);

            name = "[VCB-Studio] Spice and Wolf [NCOP][Ma10p_1080p][x265_flac]";
            result = NameParser.IsExtra(name);
            Console.WriteLine(result);
        }

    }
}