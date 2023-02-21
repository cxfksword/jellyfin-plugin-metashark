/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace AnitomySharp
{

    /// <summary>
    /// A class to manager the list of known anime keywords. This class is analogous to <c>keyword.cpp</c> of Anitomy, and <c>KeywordManager.java</c> of AnitomyJ
    /// 
    /// 本类用于管理已知动漫关键词列表
    /// 
    /// </summary>
    public static class KeywordManager
    {
        /// <summary>
        /// 包含所有关键词（大写）的内部关键词元素词典
        /// </summary>
        private static readonly Dictionary<string, Keyword> Keys = new Dictionary<string, Keyword>();
        /// <summary>
        /// 文件扩展名，无值
        /// </summary>
        private static readonly Dictionary<string, Keyword> Extensions = new Dictionary<string, Keyword>();

        /// <summary>
        /// ~~一眼真~~
        /// 
        /// 在主逻辑前预处理用的关键词集合，优先处理后被视为一个标识(token)，不会被后续操作拆散。
        /// 
        /// 如果关键词中部分字符包含在<see cref="Options.AllowedDelimiters"/>，强烈建议添加到此列表（注意：仅添加无歧义关键词）
        /// 
        /// 如果没有添加，后续处理的时候会被<see cref="Options.AllowedDelimiters"/>拆分。不过程序带了验证方法<see cref="Tokenizer.ValidateDelimiterTokens"/>，可以一定程度上重新还原关键词
        /// </summary>
        private static readonly List<Tuple<Element.ElementCategory, List<string>>> PeekEntries;

        /// <summary>
        /// 添加元素类别的关键词至<see cref="Keys"/>
        /// </summary>
        static KeywordManager()
        {
            var optionsDefault = new KeywordOptions();
            var optionsInvalid = new KeywordOptions(true, true, false);
            var optionsUnidentifiable = new KeywordOptions(false, true, true);
            var optionsUnidentifiableInvalid = new KeywordOptions(false, true, false);
            var optionsUnidentifiableUnsearchable = new KeywordOptions(false, false, true);

            Add(Element.ElementCategory.ElementAnimeSeasonPrefix,
              optionsUnidentifiable,
              new List<string> { "SAISON", "SEASON" });

            Add(Element.ElementCategory.ElementAnimeType,
              optionsUnidentifiable,
              new List<string> {
              "GEKIJOUBAN", "MOVIE",
              "OAD", "OAV", "ONA", "OVA",
              "TV",
              "番外編", "總集編","映像特典","特典","特典アニメ",
              // 特典 Special 剩下的各种类型可以全部命名成 SP，对于较特殊意义的特典也可以自定义命名
              "SPECIAL", "SPECIALS", "SP",  
              // 真人特典 Interview/Talk/Stage... 目前我们对于节目、采访、舞台活动、制作等三次元画面的长视频，一概怼成 IV。
              "IV",
              // 音乐视频 Music Video
              "MV"});

            //       add "SP" to ElementAnimeType with optionsUnidentifiable
            //       Add(Element.ElementCategory.ElementAnimeType,
            //         optionsUnidentifiableUnsearchable,
            //         new List<string> {"SP"}); // e.g. "Yumeiro Patissiere SP Professional"

            Add(Element.ElementCategory.ElementAnimeType,
              optionsUnidentifiableInvalid,
              new List<string> {
              // https://github.com/vcb-s/VCB-S_Collation/blob/master/specification.md
              // 无字 OP/ED Non-Credit Opening/Ending
              "ED", "ENDING", "NCED", "NCOP", "OP", "OPENING",
              // 预告 Preview 预告下一话内容 注意编号表示其预告的是第几话的内容而不是跟在哪一话后面
              "PREVIEW",
              // 菜单 Menu BD/DVD 播放选择菜单
              "MENU",
              // 广告 Commercial Message 电视放送广告，时长一般在 7s/15s/30s/45s/... 左右
              "CM","SPOT",
              // 宣传片/预告片 Promotion Video / Trailer 一般时长在 1~2min 命名参考原盘和 jsum
              "PV", "Teaser","TRAILER", "DRAMA",
              // 真人特典 Interview/Talk/Stage... 目前我们对于节目、采访、舞台活动、制作等三次元画面的长视频，一概怼成 IV。
              "INTERVIEW",
              "EVENT", "TOKUTEN", "LOGO"});

            Add(Element.ElementCategory.ElementAudioTerm,
              optionsDefault,
              new List<string> {
              // Audio channels
              "5.1","7.1","2CH", "6CH",
              "DTS", "DTS-ES", "DTS-MA", "DTS-HD","DTS-HDMA",
              "TRUE-HD", "TRUEHD", "THD",
              // Audio codec
              "AAC", "AACX2", "AACX3", "AACX4", "2XAAC", "3XAAC", "2AAC", "3AAC",
              "AC3", "AC3X2","AC3X3",  "EAC3", "E-AC-3",
              "FLAC", "FLACX2", "FLACX3", "FLACX4", "2XFLAC", "3XFLAC", "2FLAC", "3FLAC", "4FLAC",
              "LOSSLESS", "MP3", "OGG", "VORBIS",
              "ATMOS",
              // Audio language
              "DUAL","DUALAUDIO"
              });

            Add(Element.ElementCategory.ElementDeviceCompatibility,
              optionsDefault,
              new List<string> { "IPAD3", "IPHONE5", "IPOD", "PS3", "PS3アプコン", "XBOX", "XBOX360", "PSP" });

            Add(Element.ElementCategory.ElementDeviceCompatibility,
              optionsUnidentifiable,
              new List<string> { "ANDROID" });

            Add(Element.ElementCategory.ElementEpisodePrefix,
              optionsDefault,
              new List<string> { "EP", "EP.", "EPS", "EPS.", "EPISODE", "EPISODE.", "EPISODES", "CAPITULO", "EPISODIO", "EPIS\u00F3DIO", "FOLGE" });

            Add(Element.ElementCategory.ElementEpisodePrefix,
              optionsInvalid,
              new List<string> { "E", "\\x7B2C" }); // single-letter episode keywords are not valid tokens

            Add(Element.ElementCategory.ElementFileExtension,
              optionsDefault,
              new List<string> { "3GP", "AVI", "DIVX", "FLV", "M2TS", "MKV", "MOV", "MP4", "MPG",
              "OGM", "RM", "RMVB", "TS", "WEBM", "WMV" });

            Add(Element.ElementCategory.ElementFileExtension,
              optionsInvalid,
              new List<string> { "AAC", "AIFF", "FLAC", "M4A", "MP3", "MKA", "OGG", "WAV", "WMA", "7Z", "RAR", "ZIP", "ASS", "SRT" });

            Add(Element.ElementCategory.ElementLanguage,
              optionsDefault,
              new List<string> { "ENG", "ENGLISH", "ESPANO", "JAP", "PT-BR", "SPANISH", "VOSTFR",
              "ZH-HANS", "ZH-HANT", "CHS", "CHT", "CHN", "JPN", "JPSC", "JPTC" });

            Add(Element.ElementCategory.ElementLanguage,
              optionsUnidentifiable,
              new List<string> { "ESP", "ITA", "SC", "TC" }); // e.g. "Tokyo ESP:, "Bokura ga Ita"

            Add(Element.ElementCategory.ElementOther,
              optionsDefault,
              new List<string> { "REMASTER", "REMASTERED", "UNCUT", "TS", "VFR", "WIDESCREEN", "WS", "SPURSENGINE" });

            Add(Element.ElementCategory.ElementReleaseGroup,
              optionsDefault,
              new List<string> {
              // rip group
              "AI-RAWS","AIROTA","ANK-RAWS","ANK","ANE","AKATOMBA-RAWS","ATTKC","BEANSUB","BEATRICE-RAWS",
              "CASO","COOLCOMIC","COMMIE","DANNI","DMG","DYMY","EUPHO","EMTP-RAWS","ENKANREC","EXILED-DESTINY","FLSNOW",
              "FREEWIND","FZSD","GTX-RAWS","GST","HAKUGETSU","HQR","HKG","JYFANSUB","JSUM","KAGURA","KAMETSU",
              "KAMIGAMI-RAWS","KAMIGAMI","诸神字幕组","KNA-SUBS","KOEISUB","KTXP","LOWPOWER-RAWS","LKSUB",
              "LIUYUN","LOLIHOUSE","LITTLEBAKAS!","MABORS","MAWEN1250","MGRT","MMZY-SUB","MH","MOOZZI2",
              "PUSSUB","POPGO","PHILOSOPHY-RAWS","PPP-RAW","QTS","RARBG","RATH","REINFORCE","RUELL-NEXT","RUELL-RAWS",
              "R1RAW","SNOW-RAWS","SFEO-RAWS","SHINSEN-SUBS","SHIROKOI","SWEETSUB","SUMISORA","SOFCJ-RAWS","TSDM",
              "THORA","TUCAPTIONS","TXXZ","UCCUSS","UHA-WINGS","U2-RIP","VCB-STUDIO","VCB-S","XYX98","XKSUB","XRIP",
              "异域-11番小队","YYDM","YUSYABU","YLBUDSUB","ZAGZAD","AHU-SUB",
              "HYSUB", "SAKURATO", "SKYMOON-RAWS", "COMICAT&KISSSUB","FUSSOIR", 
              // bangumi
              "ANI", "NC-RAWS", "LILITH-RAWS", "NAN-RAWS","MINGY","NANDESUKA","KISSSUB",
              // other
              "PTER",
              // echi
              "脸肿字幕组","魔穗字幕组","桜都字幕组","MAHOXOKAZU","極彩花夢",
              // Unidentifiable
              "YUUKI"
              });

            Add(Element.ElementCategory.ElementReleaseInformation,
              optionsDefault,
              new List<string> {
              "BATCH", "COMPLETE", "PATCH", "REMUX", "REV", "REPACK", "FIN",
              "生肉", "熟肉",
              // source
              "BILIBILI","B-GLOBAL", "BAHA", "GYAO!", "U-NEXT","SENTAI"});

            Add(Element.ElementCategory.ElementReleaseInformation,
              optionsDefault,
              new List<string> {
              // echi
              "18禁", "18禁アニメ", "15禁", "無修正", "无修正", "无码", "無碼","有码", "NOWATERMARK","CENSORED","UNCENSORED","DECENSORED","有修正","无删减","未删减","有删减",
              // echi Studios
              "AIC","ANIMAC","ANIMAN","APPLE","BOOTLEG","CELEB","CHERRYLIPS","CHIPPAI","COLLABORATIONWORKS","COSMOS","DREAM（ディースリー）","DISCOVERY","DODER","EDGE（エッジ）","EDGE","EROZUKI","HILLS","HONNYBIT","JAM","JHV","JVD","MILKY蜜","MILKY","MOONROCK","MS-PICTURES","NUR","OFFICEAO","PASHMINAA","PASHMINA","PETIT","PINKPINEAPPLE","PIXY","PORO","QUEENBEE","SCHOOLZONE","SHS","SPERMATION","STARLINK","TDKコア","UNCEN","UTAMARO","VIB","ZIZ","アニアン","ひまじん","エイベックス・トラックス","オブテイン・フューチャー","クランベリー","ジャパンホームビデオ","ジュエル","バニラ","ピンクパイナップル","ファイブウェイズ","ミューズ","プリンセス・プロダクション","れもんは～と","れもんは〜と","アムモ","じゅうしぃまんご～","メリージェーン","メリー・ジェーン","せるふぃっしゅ","アームス","アスミック","フェアリーダスト","メリー_ジェーン","ばにぃうぉ～か～","ショーテン","あんてきぬすっ","エンゼルフィッシュ","オゼ","ガールズトーク","クリムゾン","サークルトリビュート","こっとんど～る","カナメプロダクション","オレンジビデオハウス","ウエスト・ケープ・コーポレーション","メリー･ジェーン","アートミック","シネマパラダイス","あかとんぼ","ディースリー","ディスカバリー","ナック映画","メディアバンク","ボイジャーエンターテイメント","ミントハウス","フレンズ","ミルクセーキ","ハニーディップ","パック・イン・ビデオ","サン出版","わるきゅ～れ＋＋","虫プロダクション","バンダイビジュアル","ハピネット・ピクチャーズXビクターエンタテインメント","ちちのや","センテスタジオ","メディアセブン","セブン","スタジオ・ファンタジア","ショコラ","カレス・コミュニケーションズ","ソフト･オン･デマンド","ソフト・オン・デマンド","セントリリア","2匹目のどぜう","37℃","北栄商事","創美企画","創美","大映映像","大映","晋遊舎","晋遊社","鈴木みら乃","虎の穴","蜜MITSU","魔人","妄想実現めでぃあ","遊人","真空間","宇宙企画"
              });

            Add(Element.ElementCategory.ElementReleaseInformation,
              optionsUnidentifiable,
              new List<string> { "END", "FINAL" }); // e.g. "The End of Evangelion", 'Final Approach"

            Add(Element.ElementCategory.ElementReleaseVersion,
              optionsDefault,
              new List<string> { "V0", "V1", "V2", "V3", "V4" });

            Add(Element.ElementCategory.ElementSource,
              optionsDefault,
              new List<string> {"BD", "BDRIP", "BD-BOX", "BDBOX", "UHD", "UHDRIP", "BLURAY", "BLU-RAY",
              "DVD", "DVD5", "DVD9", "DVD-R2J", "DVDRIP", "DVD-RIP",
              "R2DVD", "R2J", "R2JDVD", "R2JDVDRIP",
              "HDTV", "HDTVRIP", "TVRIP", "TV-RIP",
              "WEBCAST", "WEBRIP", "WEB-DL", "WEB",
              "DLRIP"});

            Add(Element.ElementCategory.ElementSubtitles,
              optionsDefault,
              new List<string> { "ASS", "GB", "BIG5", "DUB", "DUBBED", "HARDSUB", "HARDSUBS", "RAW", "SOFTSUB",
              "SOFTSUBS", "SUB", "SUBBED", "SUBTITLED" });

            Add(Element.ElementCategory.ElementVideoTerm,
              optionsDefault,
              new List<string> {
              // Frame rate
              "24FPS", "30FPS", "48FPS", "60FPS", "120FPS","SVFI",
              // Video codec
              "8BIT", "8-BIT", "10BIT", "10BITS", "10-BIT", "10-BITS",
              "HEVC-10BIT", "HEVC-YUV420P10","X264-10BIT", "X264-HI10P",
              "HI10", "HI10P", "MA10P","MA444-10P", "HI444", "HI444P", "HI444PP",
              "H264", "H265", "X264", "X265",
              "AVC", "HEVC", "HEVC2", "DIVX", "DIVX5", "DIVX6", "XVID",
              "YUV420", "YUV420P8", "YUV420P10", "YUV420P10LE", "YUV444", "YUV444P10", "YUV444P10LE","AV1",
              "MAIN10", "MAIN10P", "MAIN12", "MAIN12P",
              "HDR", "HDR10", "HMAX","DOVI","DOLBY VISION",
              // Video format
              "AVI", "RMVB", "WMV", "WMV3", "WMV9", "MKV", "MP4", "MPEG",
              // Video quality
              "HQ", "LQ",
              // Video resolution
              "UHD", "HD", "SD"});

            Add(Element.ElementCategory.ElementVolumePrefix,
              optionsDefault,
              new List<string> { "VOL", "VOL.", "VOLUME" });

            PeekEntries = new List<Tuple<Element.ElementCategory, List<string>>>
            {
              Tuple.Create(Element.ElementCategory.ElementAnimeType, new List<string> { 
              // 预告  
              "WEB PREVIEW" }),
              Tuple.Create(Element.ElementCategory.ElementAudioTerm, new List<string> { "2.0CH", "5.1CH", "7.1CH", "DTS5.1", "MA.5.1", "MA.2.0", "MA.7.1", "TRUEHD5.1", "DDP5.1", "DD5.1", "DUAL AUDIO" }),
              Tuple.Create(Element.ElementCategory.ElementVideoTerm, new List<string> { "H.264", "H264", "H.265", "X.264", "23.976FPS", "29.97FPS", "59.94FPS", "59.940FPS" }),// e.g. "H264-Flac"
              Tuple.Create(Element.ElementCategory.ElementVideoResolution, new List<string> { "480P", "720P", "1080P", "2160P", "4K", "6K", "8K" }),
              Tuple.Create(Element.ElementCategory.ElementReleaseGroup, new List<string> { "X_X", "A.I.R.NESSUB", "FUDAN_NRC", "T.H.X", "MAHO.SUB", "OKAZU.SUB", "THUNDER.SUB","ORION ORIGIN", "NEKOMOE KISSATEN" }),
              Tuple.Create(Element.ElementCategory.ElementReleaseInformation, new List<string> { 
                // echi
                "NO WATERMARK", "ALL PRODUCTS", "AN DERCEN", "BLUE EYES", "BOMB! CUTE! BOMB!", "COLLABORATION WORKS", "GREEN BUNNY", "GOLD BEAR", "HOODS ENTERTAINMENT", "HOT BEAR", "KING BEE", "PLATINUM MILKY", "MOON ROCK", "OBTAIN FUTURE", "QUEEN BEE", "SOFT DEMAND", "STUDIO ZEALOT", "SURVIVE MORE", "WHITE BEAR", "メリー ジェーン", "ビーム エンタテインメント", "蜜 -MITSU-","W.C.C.","J.A.V.N.","HSHARE.NET" })
            };
        }

        /// <summary>
        /// 字符串(<paramref name="word"/>)转换为大写
        /// </summary>
        /// <param name="word">待转换的字符串</param>
        /// <returns>返回当前字符串的大写形式</returns>
        public static string Normalize(string word)
        {
            return string.IsNullOrEmpty(word) ? word : word.ToUpperInvariant();
        }

        /// <summary>
        /// 判断元素列表中是否包含给定的字符串(<paramref name="keyword"/>)
        /// </summary>
        /// <param name="category">元素类别</param>
        /// <param name="keyword">待判断的字符串</param>
        /// <returns>`true`表示包含</returns>
        public static bool Contains(Element.ElementCategory category, string keyword)
        {
            var keys = GetKeywordContainer(category);
            if (keys.TryGetValue(keyword, out var foundEntry))
            {
                return foundEntry.Category == category;
            }

            return false;
        }

        /// <summary>
        /// Finds a particular <c>keyword</c>. If found sets <c>category</c> and <c>options</c> to the found search result.
        /// 
        /// 查找给定的关键词，并更新其元素分类和关键词配置
        /// 
        /// 如果在<see cref="Keys"/>中找到，则将<see cref="Keys"/>中此关键词对应的元素分类和关键词配置赋给给定的关键词
        /// </summary>
        /// <param name="keyword">the keyword to search for</param>
        /// <param name="category">the reference that will be set/changed to the found keyword category</param>
        /// <param name="options">the reference that will be set/changed to the found keyword options</param>
        /// <returns>true if the keyword was found</returns>
        public static bool FindAndSet(string keyword, ref Element.ElementCategory category, ref KeywordOptions options)
        {
            var keys = GetKeywordContainer(category);
            if (!keys.TryGetValue(keyword, out var foundEntry))
            {
                return false;
            }

            if (category == Element.ElementCategory.ElementUnknown)
            {
                category = foundEntry.Category;
            }
            else if (foundEntry.Category != category)
            {
                return false;
            }
            options = foundEntry.Options;
            return true;
        }

        /// <summary>
        /// Given a particular <c>filename</c> and <c>range</c> attempt to preidentify the token before we attempt the main parsing logic
        /// 
        /// 在使用主处理逻辑前，尝试对给定的文件名和范围预先确定标记(token)，关键词来自<see cref="PeekEntries"/>
        /// </summary>
        /// <param name="filename">the filename</param>
        /// <param name="range">the search range</param>
        /// <param name="elements">elements array that any pre-identified elements will be added to</param>
        /// <param name="preidentifiedTokens">elements array that any pre-identified token ranges will be added to</param>
        public static void PeekAndAdd(string filename, TokenRange range, List<Element> elements, List<TokenRange> preidentifiedTokens)
        {
            var endR = range.Offset + range.Size;
            /** 获得本次操作的字符串 */
            var search = filename.Substring(range.Offset, endR > filename.Length ? filename.Length - range.Offset : endR - range.Offset);
            foreach (var entry in PeekEntries)
            {
                foreach (var keyword in entry.Item2)
                {
                    var foundIdx = search.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase);
                    if (foundIdx == -1) continue;
                    foundIdx += range.Offset;
                    /** 将一眼真的关键字加入元素列表 */
                    elements.Add(new Element(entry.Item1, filename.Substring(foundIdx, keyword.Length)));
                    // elements.Add(new Element(entry.Item1, keyword));
                    /** 将匹配到的关键词字符串范围添加到preidentifiedTokens */
                    preidentifiedTokens.Add(new TokenRange(foundIdx, keyword.Length));
                }
            }
        }

        // Private API

        /// <summary>
        /// Returns the appropriate keyword container.
        /// 
        /// 返回合适的内部关键词元素词典<see cref="Keys"/>
        /// 
        /// 如果元素类型为文件扩展名，则返回空值的<see cref="Extensions"/>，否则返回<see cref="Keys"/>
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static Dictionary<string, Keyword> GetKeywordContainer(Element.ElementCategory category)
        {
            return category == Element.ElementCategory.ElementFileExtension ? Extensions : Keys;
        }

        /// <summary>
        /// Adds a <c>category</c>, <c>options</c>, and <c>keywords</c> to the internal keywords list.
        /// 
        /// 将元素分类、关键词配置添加给指定的关键词列表。最终形成一个内部关键词元素词典<see cref="Keys"/>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="options"></param>
        /// <param name="keywords"></param>
        private static void Add(Element.ElementCategory category, KeywordOptions options, IEnumerable<string> keywords)
        {
            var keys = GetKeywordContainer(category);
            foreach (var key in keywords.Where(k => !string.IsNullOrEmpty(k) && !keys.ContainsKey(k)))
            {
                keys[key] = new Keyword(category, options);
            }
        }
    }

    /// <summary>
    /// Keyword options for a particular keyword.
    /// 
    /// 关键词配置
    /// </summary>
    public class KeywordOptions
    {
        /// <summary>
        /// 是否可分辨，是否会产生歧义，是否会出现在动画标题中
        /// </summary>
        public bool Identifiable { get; }
        /// <summary>
        /// 是否可检索 #TODO
        /// 
        /// <see cref="ParserHelper.IsElementCategorySearchable"/>
        /// </summary>
        public bool Searchable { get; }
        /// <summary>
        /// 是否有效 #TODO
        /// </summary>
        public bool Valid { get; }

        /// <summary>
        /// 默认关键词配置：可识别，可检索，有效
        /// </summary>
        public KeywordOptions() : this(true, true, true) { }

        /// <summary>
        /// Constructs a new keyword options
        /// 
        /// 构造一个关键词配置
        /// </summary>
        /// <param name="identifiable">if the token is identifiable</param>
        /// <param name="searchable">if the token is searchable</param>
        /// <param name="valid">if the token is valid</param>
        public KeywordOptions(bool identifiable, bool searchable, bool valid)
        {
            Identifiable = identifiable;
            Searchable = searchable;
            Valid = valid;
        }

    }

    /// <summary>
    /// A Keyword 
    /// 
    /// 关键词结构体
    /// </summary>
    public struct Keyword
    {
        /// <summary>
        /// 元素类别 <see cref="Element.ElementCategory"/>
        /// </summary>
        public readonly Element.ElementCategory Category;
        /// <summary>
        /// 关键词配置 <see cref="KeywordOptions"/>
        /// </summary>
        public readonly KeywordOptions Options;

        /// <summary>
        /// Constructs a new Keyword
        /// 
        /// 构造一个新的关键词
        /// </summary>
        /// <param name="category">the category of the keyword</param>
        /// <param name="options">the keyword's options</param>
        public Keyword(Element.ElementCategory category, KeywordOptions options)
        {
            Category = category;
            Options = options;
        }
    }
}
