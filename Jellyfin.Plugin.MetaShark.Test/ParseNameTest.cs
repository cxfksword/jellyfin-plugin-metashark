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
            Assert.AreEqual(parseResult.ChineseName, "新世界");
            Assert.AreEqual(parseResult.Name, "New World");
            Assert.AreEqual(parseResult.Year, 2013);

            fileName = "V字仇杀队.V.for.Vendetta.2006";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "V字仇杀队");
            Assert.AreEqual(parseResult.Name, "V for Vendetta");
            Assert.AreEqual(parseResult.Year, 2006);


            fileName = "罗马假日.Roman.Holiday.1953.WEB-DL.1080p.x265.AAC.2Audios.GREENOTEA";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "罗马假日");
            Assert.AreEqual(parseResult.Name, "Roman Holiday");
            Assert.AreEqual(parseResult.Year, 1953);

            fileName = "【更多蓝光电影访问】红辣椒[简繁中文字幕].Paprika.2006.RERiP.1080p.BluRay.x264.DTS-WiKi";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "红辣椒");
            Assert.AreEqual(parseResult.Name, "Paprika");
            Assert.AreEqual(parseResult.Year, 2006);

            // 只英文
            fileName = "A.Chinese.Odyssey.Part.1.1995.BluRay.1080p.x265.10bit.2Audio-MiniHD";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "A Chinese Odyssey Part 1");
            Assert.AreEqual(parseResult.Year, 1995);

            fileName = "New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "New World");
            Assert.AreEqual(parseResult.Year, 2013);

            fileName = "Who.Am.I.1998.1080p.BluRay.x264.DTS-FGT";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "Who Am I");
            Assert.AreEqual(parseResult.Year, 1998);

            // 只中文
            fileName = "机动战士高达 逆袭的夏亚";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "机动战士高达 逆袭的夏亚");
            Assert.AreEqual(parseResult.Year, null);

            fileName = "秒速5厘米";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "秒速5厘米");
            Assert.AreEqual(parseResult.Year, null);


            // 标题加年份
            fileName = "V字仇杀队 (2006)";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, null);
            Assert.AreEqual(parseResult.Name, "V字仇杀队");
            Assert.AreEqual(parseResult.Year, 2006);


            // anime
            fileName = "[SAIO-Raws] もののけ姫 Mononoke Hime [BD 1920x1036 HEVC-10bit OPUSx2 AC3].mp4";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "もののけ姫");
            Assert.AreEqual(parseResult.Name, "Mononoke Hime");
            Assert.AreEqual(parseResult.Year, null);
        }

        [TestMethod]
        public void TestTVSeriesParse()
        {
            // 混合中英文
            var fileName = "航海王：狂热行动.One.Piece.Stampede.2019.BD720P.X264.AAC.Japanese&Mandarin.CHS.Mp4Ba";
            var parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "航海王：狂热行动");
            Assert.AreEqual(parseResult.Name, "One Piece Stampede");
            Assert.AreEqual(parseResult.Year, 2019);

            // 混合中英文带副标题

            // 只英文
            fileName = "She-Hulk.Attorney.at.Law.S01.1080p.WEBRip.x265-RARBG";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "She-Hulk Attorney at Law");
            Assert.AreEqual(parseResult.ParentIndexNumber, 1);
            Assert.AreEqual(parseResult.Year, null);

            fileName = "Bright.Future.S01.2022.2160p.HDR.WEB-DL.H265.AAC-BlackTV[BTBTT]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Bright Future");
            Assert.AreEqual(parseResult.ParentIndexNumber, 1);
            Assert.AreEqual(parseResult.Year, 2022);

            fileName = "Back.to.the.Future.Part.II.1989.BluRay.1080p.x265.10bit.2Audio-MiniHD[BTBTT]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Back to the Future Part II");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.Year, 1989);



            // anime混合中日文
            fileName = "[异域-11番小队][罗马浴场 THERMAE_ROMAE][1-6+SP][BDRIP][720P][X264-10bit_AAC]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "罗马浴场");
            Assert.AreEqual(parseResult.Name, "THERMAE ROMAE");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.Year, null);


            // anime
            fileName = "[Nekomoe kissaten][Shin Ikkitousen][01-03][720p][CHT]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Shin Ikkitousen");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.Year, null);

            fileName = "[SAIO-Raws] Fullmetal Alchemist Brotherhood [BD 1920x1080 HEVC-10bit OPUS][2009]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Fullmetal Alchemist Brotherhood");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.Year, 2009);
        }

        [TestMethod]
        public void TestEposideParse()
        {
            // 混合中英文
            var fileName = "新世界.New.World.2013.BluRay.1080p.x265.10bit.MNHD-FRDS";
            var parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.ChineseName, "新世界");
            Assert.AreEqual(parseResult.Name, "New World");
            Assert.AreEqual(parseResult.Year, 2013);

            // 只英文
            fileName = "She-Hulk.Attorney.At.Law.S01E01.1080p.WEBRip.x265-RARBG";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "She-Hulk Attorney At Law");
            Assert.AreEqual(parseResult.ParentIndexNumber, 1);
            Assert.AreEqual(parseResult.IndexNumber, 1);

            // 只中文
            fileName = "齊天大聖 第02集";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "齊天大聖 第02集");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.IndexNumber, 2);

            // anime
            fileName = "[YYDM-11FANS][THERMAE_ROMAE][02][BDRIP][720P][X264-10bit_AAC][7FF2269F]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "THERMAE ROMAE");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.IndexNumber, 2);

            // anime带季数
            fileName = "[WMSUB][Detective Conan - Zero‘s Tea Time ][S01][E06][BIG5][1080P].mp4";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Detective Conan - Zero‘s Tea Time");
            Assert.AreEqual(parseResult.ParentIndexNumber, 1);
            Assert.AreEqual(parseResult.IndexNumber, 6);

            fileName = "[KTXP][Machikado_Mazoku_S2][01][BIG5][1080p]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "Machikado Mazoku");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.IndexNumber, 1);

            fileName = "[異域字幕組][她和她的貓 - Everything Flows -][She and Her Cat - Everything Flows -][01][720p][繁體]";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "她和她的貓 - Everything Flows");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.IndexNumber, 1);

            // anime特典
            fileName = "[KissSub][Steins;Gate][SP][GB_BIG5_JP][BDrip][1080P][HEVC] 边界曲面的缺失之环";
            parseResult = NameParser.Parse(fileName);
            Assert.AreEqual(parseResult.Name, "边界曲面的缺失之环");
            Assert.AreEqual(parseResult.ParentIndexNumber, null);
            Assert.AreEqual(parseResult.IndexNumber, null);

        }


        [TestMethod]
        public void TestCheckExtra()
        {
            var fileName = "[VCB-Studio] Spice and Wolf [CM02][Ma10p_1080p][x265_flac]";
            var parseResult = NameParser.Parse(fileName);
            Assert.IsTrue(parseResult.IsExtra);

            fileName = "[VCB-Studio] Spice and Wolf [Menu01_2][Ma10p_1080p][x265_flac]";
            parseResult = NameParser.Parse(fileName);
            Assert.IsTrue(parseResult.IsExtra);

            fileName = "[VCB-Studio] Spice and Wolf [NCED][Ma10p_1080p][x265_flac]";
            parseResult = NameParser.Parse(fileName);
            Assert.IsTrue(parseResult.IsExtra);

            fileName = "[VCB-Studio] Spice and Wolf [NCOP][Ma10p_1080p][x265_flac]";
            parseResult = NameParser.Parse(fileName);
            Assert.IsTrue(parseResult.IsExtra);

            fileName = "[VCB-Studio] Spice and Wolf II [Drama02][Ma10p_1080p][x265_flac].mp4";
            parseResult = NameParser.Parse(fileName);
            Assert.IsTrue(parseResult.IsExtra);


        }

    }
}