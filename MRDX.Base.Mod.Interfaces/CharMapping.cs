﻿using System.Collections.Generic;
using System.Linq;

namespace MRDX.Base.Mod.Interfaces;

public static class CharMap
{
    public static readonly Dictionary<ushort, char> Forward = new()
    {
        { 0x00, ' ' },
        { 0x01, 'あ' },
        { 0x02, 'い' },
        { 0x03, 'う' },
        { 0x04, 'え' },
        { 0x05, 'お' },
        { 0x06, 'か' },
        { 0x07, 'き' },
        { 0x08, 'く' },
        { 0x09, 'け' },
        { 0x0a, 'こ' },
        { 0x0b, 'さ' },
        { 0x0c, 'し' },
        { 0x0d, 'す' },
        { 0x0e, 'せ' },
        { 0x0f, 'そ' },
        { 0x10, 'た' },
        { 0x11, 'ち' },
        { 0x12, 'つ' },
        { 0x13, 'て' },
        { 0x14, 'と' },
        { 0x15, 'な' },
        { 0x16, 'に' },
        { 0x17, 'ぬ' },
        { 0x18, 'ね' },
        { 0x19, 'の' },
        { 0x1a, 'は' },
        { 0x1b, 'ひ' },
        { 0x1c, 'ふ' },
        { 0x1d, 'へ' },
        { 0x1e, 'ほ' },
        { 0x1f, 'ま' },
        { 0x20, 'み' },
        { 0x21, 'む' },
        { 0x22, 'め' },
        { 0x23, 'も' },
        { 0x24, 'や' },
        { 0x25, 'ゆ' },
        { 0x26, 'よ' },
        { 0x27, 'ら' },
        { 0x28, 'り' },
        { 0x29, 'る' },
        { 0x2a, 'れ' },
        { 0x2b, 'ろ' },
        { 0x2c, 'わ' },
        { 0x2d, 'を' },
        { 0x2e, 'ん' },
        { 0x2f, 'が' },
        { 0x30, 'ぎ' },
        { 0x31, 'ぐ' },
        { 0x32, 'げ' },
        { 0x33, 'ご' },
        { 0x34, 'ざ' },
        { 0x35, 'じ' },
        { 0x36, 'ず' },
        { 0x37, 'ぜ' },
        { 0x38, 'ぞ' },
        { 0x39, 'だ' },
        { 0x3a, 'ぢ' },
        { 0x3b, 'づ' },
        { 0x3c, 'で' },
        { 0x3d, 'ど' },
        { 0x3e, 'ば' },
        { 0x3f, 'び' },
        { 0x40, 'ぶ' },
        { 0x41, 'べ' },
        { 0x42, 'ぼ' },
        { 0x43, 'ぁ' },
        { 0x44, 'ぃ' },
        { 0x45, 'ぅ' },
        { 0x46, 'ぇ' },
        { 0x47, 'ぉ' },
        { 0x48, 'ゃ' },
        { 0x49, 'ゅ' },
        { 0x4a, 'ょ' },
        { 0x4b, 'ぱ' },
        { 0x4c, 'ぴ' },
        { 0x4d, 'ぷ' },
        { 0x4e, 'ぺ' },
        { 0x4f, 'ぽ' },
        { 0x50, 'っ' },
        { 0x51, 'ア' },
        { 0x52, 'イ' },
        { 0x53, 'ウ' },
        { 0x54, 'エ' },
        { 0x55, 'オ' },
        { 0x56, 'カ' },
        { 0x57, 'キ' },
        { 0x58, 'ク' },
        { 0x59, 'ケ' },
        { 0x5a, 'コ' },
        { 0x5b, 'サ' },
        { 0x5c, 'シ' },
        { 0x5d, 'ス' },
        { 0x5e, 'セ' },
        { 0x5f, 'ソ' },
        { 0x60, 'タ' },
        { 0x61, 'チ' },
        { 0x62, 'ツ' },
        { 0x63, 'テ' },
        { 0x64, 'ト' },
        { 0x65, 'ナ' },
        { 0x66, 'ニ' },
        { 0x67, 'ヌ' },
        { 0x68, 'ネ' },
        { 0x69, 'ノ' },
        { 0x6a, 'ハ' },
        { 0x6b, 'ヒ' },
        { 0x6c, 'フ' },
        { 0x6d, 'ヘ' },
        { 0x6e, 'ホ' },
        { 0x6f, 'マ' },
        { 0x70, 'ミ' },
        { 0x71, 'ム' },
        { 0x72, 'メ' },
        { 0x73, 'モ' },
        { 0x74, 'ヤ' },
        { 0x75, 'ユ' },
        { 0x76, 'ヨ' },
        { 0x77, 'ラ' },
        { 0x78, 'リ' },
        { 0x79, 'ル' },
        { 0x7a, 'レ' },
        { 0x7b, 'ロ' },
        { 0x7c, 'ワ' },
        { 0x7d, 'ヲ' },
        { 0x7e, 'ン' },
        { 0x7f, 'ガ' },
        { 0x80, 'ギ' },
        { 0x81, 'グ' },
        { 0x82, 'ゲ' },
        { 0x83, 'ゴ' },
        { 0x84, 'ザ' },
        { 0x85, 'ジ' },
        { 0x86, 'ズ' },
        { 0x87, 'ゼ' },
        { 0x88, 'ゾ' },
        { 0x89, 'ダ' },
        { 0x8a, 'ヂ' },
        { 0x8b, 'ヅ' },
        { 0x8c, 'デ' },
        { 0x8d, 'ド' },
        { 0x8e, 'バ' },
        { 0x8f, 'ビ' },
        { 0x90, 'ブ' },
        { 0x91, 'ベ' },
        { 0x92, 'ボ' },
        { 0x93, 'ァ' },
        { 0x94, 'ィ' },
        { 0x95, 'ゥ' },
        { 0x96, 'ェ' },
        { 0x97, 'ォ' },
        { 0x98, 'ャ' },
        { 0x99, 'ュ' },
        { 0x9a, 'ョ' },
        { 0x9b, 'パ' },
        { 0x9c, 'ピ' },
        { 0x9d, 'プ' },
        { 0x9e, 'ペ' },
        { 0x9f, 'ポ' },
        { 0xa0, 'ッ' },
        { 0xa1, 'ヴ' },
        { 0xa2, '0' },
        { 0xa3, '1' },
        { 0xa4, '2' },
        { 0xa5, '3' },
        { 0xa6, '4' },
        { 0xa7, '5' },
        { 0xa8, '6' },
        { 0xa9, '7' },
        { 0xaa, '8' },
        { 0xab, '9' },
        { 0xb000, 'A' },
        { 0xb001, 'B' },
        { 0xb002, 'C' },
        { 0xb003, 'D' },
        { 0xb004, 'E' },
        { 0xb005, 'F' },
        { 0xb006, 'G' },
        { 0xb007, 'H' },
        { 0xb008, 'I' },
        { 0xb009, 'J' },
        { 0xb00a, 'K' },
        { 0xb00b, 'L' },
        { 0xb00c, 'M' },
        { 0xb00d, 'N' },
        { 0xb00e, 'O' },
        { 0xb00f, 'P' },
        { 0xb010, 'Q' },
        { 0xb011, 'R' },
        { 0xb012, 'S' },
        { 0xb013, 'T' },
        { 0xb014, 'U' },
        { 0xb015, 'V' },
        { 0xb016, 'W' },
        { 0xb017, 'X' },
        { 0xb018, 'Y' },
        { 0xb019, 'Z' },
        { 0xb01a, 'a' },
        { 0xb01b, 'b' },
        { 0xb01c, 'c' },
        { 0xb01d, 'd' },
        { 0xb01e, 'e' },
        { 0xb01f, 'f' },
        { 0xb020, 'g' },
        { 0xb021, 'h' },
        { 0xb022, 'i' },
        { 0xb023, 'j' },
        { 0xb024, 'k' },
        { 0xb025, 'l' },
        { 0xb026, 'm' },
        { 0xb027, 'n' },
        { 0xb028, 'o' },
        { 0xb029, 'p' },
        { 0xb02a, 'q' },
        { 0xb02b, 'r' },
        { 0xb02c, 's' },
        { 0xb02d, 't' },
        { 0xb02e, 'u' },
        { 0xb02f, 'v' },
        { 0xb030, 'w' },
        { 0xb031, 'x' },
        { 0xb032, 'y' },
        { 0xb033, 'z' },
        { 0xb034, '.' },
        { 0xb035, '·' },
        { 0xb036, '!' },
        { 0xb037, '?' },
        { 0xb038, '—' },
        { 0xb039, ',' },
        { 0xb03a, '◦' },
        { 0xb03b, '+' },
        { 0xb03c, '@' },
        { 0xb03d, '/' },
        { 0xb03e, '-' },
        { 0xb03f, '%' },
        { 0xb040, '=' },
        { 0xb041, '⎡' },
        { 0xb042, '⎦' },
        { 0xb043, '○' },
        { 0xb044, '△' },
        { 0xb045, '□' },
        { 0xb046, '×' },
        { 0xb047, '▲' },
        { 0xb048, '▼' },
        { 0xb049, '\'' },
        { 0xb04a, '"' },
        { 0xb04b, ':' },
        { 0xb04c, '~' },
        { 0xb04d, '←' },
        { 0xb04e, '→' },
        { 0xb04f, '(' },
        { 0xb050, ')' },
        { 0xb051, '÷' },
        { 0xb052, '〒' },
        { 0xb053, '◎' },
        { 0xb054, '↑' },
        { 0xb055, '■' },
        { 0xb056, '↓' },
        { 0xb057, '夜' },
        { 0xb058, '温' },
        { 0xb059, '応' },
        { 0xb05a, '色' },
        { 0xb05b, '勇' },
        { 0xb05c, '喜' },
        { 0xb05d, '駄' },
        { 0xb05e, '脂' },
        { 0xb05f, '肪' },
        { 0xb060, '栄' },
        { 0xb061, '緊' },
        { 0xb062, '担' },
        { 0xb063, '筋' },
        { 0xb064, '固' },
        { 0xb065, '柔' },
        { 0xb066, '軟' },
        { 0xb067, '皮' },
        { 0xb068, '硬' },
        { 0xb069, '質' },
        { 0xb06a, '奮' },
        { 0xb06b, '告' },
        { 0xb06c, '詳' },
        { 0xb06d, '細' },
        { 0xb06e, '壁' },
        { 0xb06f, '末' },
        { 0xb070, '飲' },
        { 0xb071, '償' },
        { 0xb072, '飛' },
        { 0xb073, '躍' },
        { 0xb074, '吹' },
        { 0xb075, '非' },
        { 0xb076, '常' },
        { 0xb077, '野' },
        { 0xb078, '過' },
        { 0xb079, '去' },
        { 0xb07a, '貸' },
        { 0xb07b, '材' },
        { 0xb07c, '紙' },
        { 0xb07d, '身' },
        { 0xb07e, '装' },
        { 0xb07f, '飾' },
        { 0xb080, '男' },
        { 0xb081, '札' },
        { 0xb082, '甘' },
        { 0xb083, '主' },
        { 0xb084, '海' },
        { 0xb085, '囲' },
        { 0xb086, '祝' },
        { 0xb087, '未' },
        { 0xb088, '急' },
        { 0xb089, '宅' },
        { 0xb08a, '店' },
        { 0xb08b, '充' },
        { 0xb08c, '悔' },
        { 0xb08d, '勤' },
        { 0xb08e, '素' },
        { 0xb08f, '接' },
        { 0xb090, '申' },
        { 0xb091, '況' },
        { 0xb092, '追' },
        { 0xb093, '届' },
        { 0xb094, '若' },
        { 0xb095, '季' },
        { 0xb096, '節' },
        { 0xb097, '誉' },
        { 0xb098, '善' },
        { 0xb099, '交' },
        { 0xb0a0, '術' },
        { 0xb0a1, '攻' },
        { 0xb0a2, '撃' },
        { 0xb0a3, '防' },
        { 0xb0a4, '御' },
        { 0xb0a5, '知' },
        { 0xb0a6, '能' },
        { 0xb0a7, '運' },
        { 0xb0a8, '気' },
        { 0xb0a9, '忠' },
        { 0xb0aa, '誠' },
        { 0xb0ab, '疲' },
        { 0xb0ac, '労' },
        { 0xb0ad, '値' },
        { 0xb0ae, '敗' },
        { 0xb0af, '額' },
        { 0xb0b0, '表' },
        { 0xb0b1, '側' },
        { 0xb0b2, '向' },
        { 0xb0b3, '押' },
        { 0xb0b4, '基' },
        { 0xb0b5, '本' },
        { 0xb0b6, '上' },
        { 0xb0b7, '下' },
        { 0xb0b8, '左' },
        { 0xb0b9, '右' },
        { 0xb0ba, '前' },
        { 0xb0bb, '後' },
        { 0xb0bc, '必' },
        { 0xb0bd, '殺' },
        { 0xb0be, '合' },
        { 0xb0bf, '体' },
        { 0xb0c0, '円' },
        { 0xb0c1, '盤' },
        { 0xb0c2, '石' },
        { 0xb0c3, '大' },
        { 0xb0c4, '中' },
        { 0xb0c5, '小' },
        { 0xb0c6, '会' },
        { 0xb0c7, '出' },
        { 0xb0c8, '場' },
        { 0xb0c9, '歳' },
        { 0xb0ca, '勝' },
        { 0xb0cb, '賞' },
        { 0xb0cc, '金' },
        { 0xb0cd, '放' },
        { 0xb0ce, '牧' },
        { 0xb0cf, '費' },
        { 0xb0d0, '用' },
        { 0xb0d1, '参' },
        { 0xb0d2, '加' },
        { 0xb0d3, '不' },
        { 0xb0d4, '可' },
        { 0xb0d5, '入' },
        { 0xb0d6, '院' },
        { 0xb0d7, '逃' },
        { 0xb0d8, '日' },
        { 0xb0d9, '付' },
        { 0xb0da, '進' },
        { 0xb0db, '化' },
        { 0xb0dc, '再' },
        { 0xb0dd, '別' },
        { 0xb0de, '選' },
        { 0xb0df, '実' },
        { 0xb0e0, '行' },
        { 0xb0e1, '今' },
        { 0xb0e2, '年' },
        { 0xb0e3, '売' },
        { 0xb0e4, '切' },
        { 0xb0e5, '新' },
        { 0xb0e6, '購' },
        { 0xb0e7, '手' },
        { 0xb0e8, '持' },
        { 0xb0e9, '却' },
        { 0xb0ea, '成' },
        { 0xb0eb, '作' },
        { 0xb0ec, '登' },
        { 0xb0ed, '録' },
        { 0xb0ee, '誕' },
        { 0xb0ef, '生' },
        { 0xb0f0, '祭' },
        { 0xb0f1, '終' },
        { 0xb0f2, '了' },
        { 0xb0f3, '帰' },
        { 0xb0f4, '来' },
        { 0xb0f5, '何' },
        { 0xb0f6, '戻' },
        { 0xb0f7, '育' },
        { 0xb0f8, '一' },
        { 0xb0f9, '度' },
        { 0xb0fa, '払' },
        { 0xb0fb, '最' },
        { 0xb0fc, '悪' },
        { 0xb0fd, '言' },
        { 0xb0fe, '市' },
        { 0xb0ff, '揃' },
        { 0xb100, '有' },
        { 0xb101, '効' },
        { 0xb102, '活' },
        { 0xb103, '蔵' },
        { 0xb104, '空' },
        { 0xb105, '他' },
        { 0xb106, '足' },
        { 0xb107, '所' },
        { 0xb108, '優' },
        { 0xb109, '料' },
        { 0xb10a, '取' },
        { 0xb10b, '保' },
        { 0xb10c, '管' },
        { 0xb10d, '続' },
        { 0xb10e, '止' },
        { 0xb10f, '引' },
        { 0xb110, '退' },
        { 0xb111, '決' },
        { 0xb112, '定' },
        { 0xb113, '画' },
        { 0xb114, '面' },
        { 0xb115, '発' },
        { 0xb116, '見' },
        { 0xb117, '州' },
        { 0xb118, '古' },
        { 0xb119, '代' },
        { 0xb11a, '遺' },
        { 0xb11b, '跡' },
        { 0xb11c, '国' },
        { 0xb11d, '伝' },
        { 0xb11e, '説' },
        { 0xb11f, '当' },
        { 0xb120, '時' },
        { 0xb121, '王' },
        { 0xb122, '世' },
        { 0xb123, '様' },
        { 0xb124, '版' },
        { 0xb125, '封' },
        { 0xb126, '印' },
        { 0xb127, '魂' },
        { 0xb128, '神' },
        { 0xb129, '我' },
        { 0xb12a, '等' },
        { 0xb12b, '与' },
        { 0xb12c, '人' },
        { 0xb12d, '話' },
        { 0xb12e, '長' },
        { 0xb12f, '思' },
        { 0xb130, '平' },
        { 0xb131, '原' },
        { 0xb132, '学' },
        { 0xb133, '者' },
        { 0xb134, '研' },
        { 0xb135, '究' },
        { 0xb136, '遊' },
        { 0xb137, '仕' },
        { 0xb138, '事' },
        { 0xb139, '各' },
        { 0xb13a, '地' },
        { 0xb13b, '々' },
        { 0xb13c, '強' },
        { 0xb13d, '戦' },
        { 0xb13e, '第' },
        { 0xb13f, '回' },
        { 0xb140, '記' },
        { 0xb141, '念' },
        { 0xb142, '写' },
        { 0xb143, '真' },
        { 0xb144, '公' },
        { 0xb145, '式' },
        { 0xb146, '競' },
        { 0xb147, '技' },
        { 0xb148, '万' },
        { 0xb149, '観' },
        { 0xb14a, '客' },
        { 0xb14b, '専' },
        { 0xb14c, '門' },
        { 0xb14d, '現' },
        { 0xb14e, '花' },
        { 0xb14f, '形' },
        { 0xb150, '職' },
        { 0xb151, '業' },
        { 0xb152, '明' },
        { 0xb153, '聞' },
        { 0xb154, '並' },
        { 0xb155, '抵' },
        { 0xb156, '孫' },
        { 0xb157, '娘' },
        { 0xb158, '負' },
        { 0xb159, '特' },
        { 0xb15a, '徴' },
        { 0xb15b, '肝' },
        { 0xb15c, '位' },
        { 0xb15d, '心' },
        { 0xb15e, '経' },
        { 0xb15f, '験' },
        { 0xb160, '豪' },
        { 0xb161, '精' },
        { 0xb162, '近' },
        { 0xb163, '予' },
        { 0xb164, '製' },
        { 0xb165, '品' },
        { 0xb166, '腕' },
        { 0xb167, '買' },
        { 0xb168, '屋' },
        { 0xb169, '休' },
        { 0xb16a, '使' },
        { 0xb16b, '殿' },
        { 0xb16c, '堂' },
        { 0xb16d, '種' },
        { 0xb16e, '項' },
        { 0xb16f, '目' },
        { 0xb170, '具' },
        { 0xb171, '間' },
        { 0xb172, '抜' },
        { 0xb173, '山' },
        { 0xb174, '棒' },
        { 0xb175, '森' },
        { 0xb176, '建' },
        { 0xb177, '設' },
        { 0xb178, '炭' },
        { 0xb179, '坑' },
        { 0xb17a, '郵' },
        { 0xb17b, '便' },
        { 0xb17c, '配' },
        { 0xb17d, '達' },
        { 0xb17e, '東' },
        { 0xb17f, '西' },
        { 0xb180, '南' },
        { 0xb181, '北' },
        { 0xb182, '方' },
        { 0xb183, '探' },
        { 0xb184, '険' },
        { 0xb185, '養' },
        { 0xb186, '的' },
        { 0xb187, '指' },
        { 0xb188, '怪' },
        { 0xb189, '力' },
        { 0xb18a, '車' },
        { 0xb18b, '狩' },
        { 0xb18c, '畑' },
        { 0xb18d, '家' },
        { 0xb18e, '畜' },
        { 0xb18f, '番' },
        { 0xb190, '頭' },
        { 0xb191, '重' },
        { 0xb192, '軽' },
        { 0xb193, '傷' },
        { 0xb194, '死' },
        { 0xb195, '名' },
        { 0xb196, '動' },
        { 0xb197, '調' },
        { 0xb198, '子' },
        { 0xb199, '冷' },
        { 0xb19a, '淡' },
        { 0xb19b, '混' },
        { 0xb19c, '乱' },
        { 0xb19d, '吐' },
        { 0xb19e, '刺' },
        { 0xb19f, '熱' },
        { 0xb1a0, '天' },
        { 0xb1a1, '連' },
        { 0xb1a2, '転' },
        { 0xb1a3, '玉' },
        { 0xb1a4, '闘' },
        { 0xb1a5, '震' },
        { 0xb1a6, '竜' },
        { 0xb1a7, '巻' },
        { 0xb1a8, '投' },
        { 0xb1a9, '岩' },
        { 0xb1aa, '刀' },
        { 0xb1ab, '弓' },
        { 0xb1ac, '矢' },
        { 0xb1ad, '雷' },
        { 0xb1ae, '召' },
        { 0xb1af, '喚' },
        { 0xb1b0, '復' },
        { 0xb1b1, '魔' },
        { 0xb1b2, '法' },
        { 0xb1b3, '針' },
        { 0xb1b4, '串' },
        { 0xb1b5, '牙' },
        { 0xb1b6, '毒' },
        { 0xb1b7, '煙' },
        { 0xb1b8, '幕' },
        { 0xb1b9, '元' },
        { 0xb1ba, '類' },
        { 0xb1bb, '少' },
        { 0xb1bc, '突' },
        { 0xb1bd, '変' },
        { 0xb1be, '水' },
        { 0xb1bf, '鉄' },
        { 0xb1c0, '砲' },
        { 0xb1c1, '恐' },
        { 0xb1c2, '怖' },
        { 0xb1c3, '歌' },
        { 0xb1c4, '視' },
        { 0xb1c5, '線' },
        { 0xb1c6, '声' },
        { 0xb1c7, '巴' },
        { 0xb1c8, '根' },
        { 0xb1c9, '性' },
        { 0xb1ca, '注' },
        { 0xb1cb, '意' },
        { 0xb1cc, '存' },
        { 0xb1cd, '在' },
        { 0xb1ce, '食' },
        { 0xb1cf, '物' },
        { 0xb1d0, '道' },
        { 0xb1d1, '息' },
        { 0xb1d2, '功' },
        { 0xb1d3, '失' },
        { 0xb1d4, '掘' },
        { 0xb1d5, '同' },
        { 0xb1d6, '士' },
        { 0xb1d7, '始' },
        { 0xb1d8, '親' },
        { 0xb1d9, '憧' },
        { 0xb1da, '歩' },
        { 0xb1db, '月' },
        { 0xb1dc, '捨' },
        { 0xb1dd, '立' },
        { 0xb1de, '派' },
        { 0xb1df, '要' },
        { 0xb1e0, '寄' },
        { 0xb1e1, '無' },
        { 0xb1e2, '脳' },
        { 0xb1e3, '夢' },
        { 0xb1e4, '対' },
        { 0xb1e5, '丈' },
        { 0xb1e6, '夫' },
        { 0xb1e7, '既' },
        { 0xb1e8, '好' },
        { 0xb1e9, '遠' },
        { 0xb1ea, '工' },
        { 0xb1eb, '房' },
        { 0xb1ec, '働' },
        { 0xb1ed, '初' },
        { 0xb1ee, '程' },
        { 0xb1ef, '暖' },
        { 0xb1f0, '協' },
        { 0xb1f1, '以' },
        { 0xb1f2, '外' },
        { 0xb1f3, '週' },
        { 0xb1f4, '理' },
        { 0xb1f5, '信' },
        { 0xb1f6, '頼' },
        { 0xb1f7, '買' },
        { 0xb1f8, '資' },
        { 0xb1f9, '格' },
        { 0xb1fa, '自' },
        { 0xb1fb, '分' },
        { 0xb1fc, '隠' },
        { 0xb1fd, '嫌' },
        { 0xb1fe, '怒' },
        { 0xb1ff, '為' },
        { 0xb200, '相' },
        { 0xb201, '比' },
        { 0xb202, '率' },
        { 0xb203, '違' },
        { 0xb204, '関' },
        { 0xb205, '係' },
        { 0xb206, '迷' },
        { 0xb207, '惑' },
        { 0xb208, '準' },
        { 0xb209, '敵' },
        { 0xb20a, '残' },
        { 0xb20b, '総' },
        { 0xb20c, '英' },
        { 0xb20d, '数' },
        { 0xb20e, '吠' },
        { 0xb20f, '砂' },
        { 0xb210, '火' },
        { 0xb211, '弾' },
        { 0xb212, '超' },
        { 0xb213, '尻' },
        { 0xb214, '尾' },
        { 0xb215, '往' },
        { 0xb216, '斬' },
        { 0xb217, '虫' },
        { 0xb218, '剤' },
        { 0xb219, '風' },
        { 0xb21a, '船' },
        { 0xb21b, '背' },
        { 0xb21c, '削' },
        { 0xb21d, '除' },
        { 0xb21e, '冬' },
        { 0xb21f, '眠' },
        { 0xb220, '情' },
        { 0xb221, '報' },
        { 0xb222, '銀' },
        { 0xb223, '状' },
        { 0xb224, '態' },
        { 0xb225, '処' },
        { 0xb226, '考' },
        { 0xb227, '結' },
        { 0xb228, '果' },
        { 0xb229, '安' },
        { 0xb22a, '希' },
        { 0xb22b, '望' },
        { 0xb22c, '電' },
        { 0xb22d, '磁' },
        { 0xb22e, '街' },
        { 0xb22f, '修' },
        { 0xb230, '鉱' },
        { 0xb231, '先' },
        { 0xb232, '賃' },
        { 0xb233, '貸' },
        { 0xb234, '抗' },
        { 0xb235, '命' },
        { 0xb236, '避' },
        { 0xb237, '純' },
        { 0xb238, '血' },
        { 0xb239, '統' },
        { 0xb23a, '高' },
        { 0xb23b, '忘' },
        { 0xb23c, '直' },
        { 0xb23d, '支' },
        { 0xb23e, '消' },
        { 0xb23f, 'ヶ' },
        { 0xb240, '難' },
        { 0xb241, '易' },
        { 0xb242, '務' },
        { 0xb243, '図' },
        { 0xb244, '病' },
        { 0xb245, '崩' },
        { 0xb246, '頑' },
        { 0xb247, '張' },
        { 0xb248, '励' },
        { 0xb249, '叱' },
        { 0xb24a, '苦' },
        { 0xb24b, '全' },
        { 0xb24c, '商' },
        { 0xb24d, '幅' },
        { 0xb24e, '断' },
        { 0xb24f, '書' },
        { 0xb250, '留' },
        { 0xb251, '願' },
        { 0xb252, '確' },
        { 0xb253, '礼' },
        { 0xb254, '増' },
        { 0xb255, '段' },
        { 0xb256, '愛' },
        { 0xb257, '君' },
        { 0xb258, '流' },
        { 0xb259, '仲' },
        { 0xb25a, '感' },
        { 0xb25b, '限' },
        { 0xb25c, '副' },
        { 0xb25d, '多' },
        { 0xb25e, '績' },
        { 0xb25f, '適' },
        { 0xb260, '欲' },
        { 0xb261, '内' },
        { 0xb262, '判' },
        { 0xb263, '利' },
        { 0xb264, '開' },
        { 0xb265, '激' },
        { 0xb266, '期' },
        { 0xb267, '待' },
        { 0xb268, '反' },
        { 0xb269, '省' },
        { 0xb26a, '友' },
        { 0xb26b, '緒' },
        { 0xb26c, '絶' },
        { 0xb26d, '提' },
        { 0xb26e, '供' },
        { 0xb26f, '土' },
        { 0xb270, '溺' },
        { 0xb271, '棄' },
        { 0xb272, '権' },
        { 0xb273, '打' },
        { 0xb274, '催' },
        { 0xb275, '試' },
        { 0xb276, '距' },
        { 0xb277, '離' },
        { 0xb278, '光' },
        { 0xb279, '聖' },
        { 0xb27a, '根' },
        { 0xb27b, '花' },
        { 0xb27c, '粉' },
        { 0xb27d, '種' },
        { 0xb27e, '蜜' },
        { 0xb27f, '草' },
        { 0xb280, '香' },
        { 0xb281, '餅' },
        { 0xb282, '美' },
        { 0xb283, '走' },
        { 0xb284, '丸' },
        { 0xb285, '示' },
        { 0xb286, '植' },
        { 0xb287, '部' },
        { 0xb288, '清' },
        { 0xb289, '味' },
        { 0xb28a, '覚' },
        { 0xb28b, '黒' },
        { 0xb28c, '焼' },
        { 0xb28d, '薬' },
        { 0xb28e, '返' },
        { 0xb28f, '寿' },
        { 0xb290, '葬' },
        { 0xb291, '亡' },
        { 0xb292, '寺' },
        { 0xb293, '荷' },
        { 0xb294, '信' },
        { 0xb295, '条' },
        { 0xb296, '件' },
        { 0xb297, '危' },
        { 0xb298, '春' },
        { 0xb299, '夏' },
        { 0xb29a, '秋' },
        { 0xb29b, '杯' },
        { 0xb29c, '木' },
        { 0xb29d, '招' },
        { 0xb29e, '黄' },
        { 0xb29f, '卵' },
        { 0xb2a0, '偶' },
        { 0xb2a1, '像' },
        { 0xb2a2, '鑑' },
        { 0xb2a3, '底' },
        { 0xb2a4, '脇' },
        { 0xb2a5, '表' },
        { 0xb2a6, '儀' },
        { 0xb2a7, '奥' },
        { 0xb2a8, '校' },
        { 0xb2a9, '貴' },
        { 0xb2aa, '雪' },
        { 0xb2ab, '口' },
        { 0xb2ac, '査' },
        { 0xb2ad, '役' },
        { 0xb2ae, '亡' },
        { 0xb2af, '移' },
        { 0xb2b0, '評' },
        { 0xb2b1, '昇' },
        { 0xb2b2, '均' },
        { 0xb2b3, '般' },
        { 0xb2b4, '欠' },
        { 0xb2b5, '随' },
        { 0xb2b6, '圧' },
        { 0xb2b7, '倒' },
        { 0xb2b8, '致' },
        { 0xb2b9, '守' },
        { 0xb2ba, '低' },
        { 0xb2bb, '潜' },
        { 0xb2bc, '点' },
        { 0xb2bd, '壊' },
        { 0xb2be, '正' },
        { 0xb2bf, '族' },
        { 0xb2c0, '文' },
        { 0xb2c1, '句' },
        { 0xb2c2, '両' },
        { 0xb2c3, '得' },
        { 0xb2c4, '界' },
        { 0xb2c5, '次' },
        { 0xb2c6, '毛' },
        { 0xb2c7, '級' },
        { 0xb2c8, '影' },
        { 0xb2c9, '響' },
        { 0xb2ca, '模' },
        { 0xb2cb, '狂' },
        { 0xb2cc, '暴' },
        { 0xb2cd, '破' },
        { 0xb2ce, '否' },
        { 0xb2cf, '太' },
        { 0xb2d0, '魅' },
        { 0xb2d1, '女' },
        { 0xb2d2, '容' },
        { 0xb2d3, '姿' },
        { 0xb2d4, '昔' },
        { 0xb2d5, '向' },
        { 0xb2d6, '肉' },
        { 0xb2d7, '星' },
        { 0xb2d8, '歓' },
        { 0xb2d9, '価' },
        { 0xb2da, '構' },
        { 0xb2db, '勉' },
        { 0xb2dc, '習' },
        { 0xb2dd, '教' },
        { 0xb2de, '秒' },
        { 0xb2df, '通' },
        { 0xb2e0, '音' },
        { 0xb2e1, '楽' },
        { 0xb2e2, '館' },
        { 0xb2e3, '兵' },
        { 0xb2e4, '隊' },
        { 0xb2e5, '争' },
        { 0xb2e6, '集' },
        { 0xb2e7, '嵐' },
        { 0xb2e8, '誰' },
        { 0xb2e9, '那' },
        { 0xb2ea, '興' },
        { 0xb2eb, '半' },
        { 0xb2ec, '字' },
        { 0xb2ed, '読' },
        { 0xb2ee, '早' },
        { 0xb2ef, '墓' },
        { 0xb2f0, '域' },
        { 0xb2f1, '宮' },
        { 0xb2f2, '台' },
        { 0xb2f3, '困' },
        { 0xb2f4, '渡' },
        { 0xb2f5, '横' },
        { 0xb2f6, '官' },
        { 0xb2f7, '四' },
        { 0xb2f8, '角' },
        { 0xb2f9, '白' },
        { 0xb2fa, '羽' },
        { 0xb2fb, '柱' },
        { 0xb2fc, '陽' },
        { 0xb2fd, '晩' },
        { 0xb2fe, '局' },
        { 0xb2ff, '審' },
        { 0xb300, '組' },
        { 0xb301, '遣' },
        { 0xb302, '呼' },
        { 0xb303, '己' },
        { 0xb304, '紹' },
        { 0xb305, '介' },
        { 0xb306, '助' },
        { 0xb307, '偉' },
        { 0xb308, '炉' },
        { 0xb309, '鏡' },
        { 0xb30a, '産' },
        { 0xb30b, '獲' },
        { 0xb30c, '似' },
        { 0xb30d, '触' },
        { 0xb30e, '鋭' },
        { 0xb30f, '緑' },
        { 0xb310, '伸' },
        { 0xb311, '縮' },
        { 0xb312, '区' },
        { 0xb313, '耳' },
        { 0xb314, '瞳' },
        { 0xb315, '赤' },
        { 0xb316, '映' },
        { 0xb317, '乗' },
        { 0xb318, '腹' },
        { 0xb319, '飼' },
        { 0xb31a, '暑' },
        { 0xb31b, '器' },
        { 0xb31c, '青' },
        { 0xb31d, '憶' },
        { 0xb31e, '周' },
        { 0xb31f, '禁' },
        { 0xb320, '顔' },
        { 0xb321, '然' },
        { 0xb322, '護' },
        { 0xb323, '仮' },
        { 0xb324, '暗' },
        { 0xb325, '義' },
        { 0xb326, '置' },
        { 0xb327, '祖' },
        { 0xb328, '首' },
        { 0xb329, '歴' },
        { 0xb32a, '史' },
        { 0xb32b, '汚' },
        { 0xb32c, '創' },
        { 0xb32d, '造' },
        { 0xb32e, '章' },
        { 0xb32f, '識' },
        { 0xb330, '解' },
        { 0xb331, '送' },
        { 0xb332, '愛' },
        { 0xb333, '受' },
        { 0xb334, '二' },
        { 0xb335, '深' },
        { 0xb336, '満' },
        { 0xb337, '猛' },
        { 0xb338, '込' },
        { 0xb339, '巨' },
        { 0xb33a, '瞑' },
        { 0xb33b, '想' },
        { 0xb33c, '路' },
        { 0xb33d, '脱' },
        { 0xb33e, '証' },
        { 0xb33f, '操' },
        { 0xb340, '令' },
        { 0xb341, '泣' },
        { 0xb342, '笑' },
        { 0xb343, '陸' },
        { 0xb344, '宙' },
        { 0xb345, '換' },
        { 0xb346, '認' },
        { 0xb347, '完' },
        { 0xb348, '剣' },
        { 0xb349, '健' },
        { 0xb34a, '康' },
        { 0xb34b, '双' },
        { 0xb34c, '差' },
        { 0xb34d, '洗' },
        { 0xb34e, '氷' },
        { 0xb34f, '輸' },
        { 0xb350, '散' },
        { 0xb351, '煎' },
        { 0xb352, '含' },
        { 0xb353, '拠' },
        { 0xb354, '減' },
        { 0xb355, '丹' },
        { 0xb356, '烈' },
        { 0xb357, '陳' },
        { 0xb358, '謝' },
        { 0xb359, '武' },
        { 0xb35a, '刃' },
        { 0xb35b, '寒' },
        { 0xb35c, '狭' },
        { 0xb35d, '窓' },
        { 0xb35e, '梁' },
        { 0xb35f, '良' },
        { 0xb360, '宿' },
        { 0xb361, '損' },
        { 0xb362, '痛' },
        { 0xb363, '照' },
        { 0xb364, '速' },
        { 0xb365, '泊' },
        { 0xb366, '景' },
        { 0xb367, '企' },
        { 0xb368, '秘' },
        { 0xb369, '密' },
        { 0xb36a, '住' },
        { 0xb36b, '只' },
        { 0xb36c, '老' },
        { 0xb36d, '静' },
        { 0xb36e, '父' },
        { 0xb36f, '絡' },
        { 0xb370, '災' },
        { 0xb371, '祈' },
        { 0xb372, '桜' },
        { 0xb373, '百' },
        { 0xb374, '許' },
        { 0xb375, '枚' },
        { 0xb376, '缶' },
        { 0xb377, '詰' },
        { 0xb378, '広' },
        { 0xb379, '炎' },
        { 0xb37a, '択' },
        { 0xb37b, '床' },
        { 0xb37c, '七' },
        { 0xb37d, '村' },
        { 0xb37e, '酒' },
        { 0xb37f, '順' },
        { 0xb380, '型' },
        { 0xb381, '板' },
        { 0xb382, '岸' },
        { 0xb383, '漠' },
        { 0xb384, '凍' },
        { 0xb385, '解' },
        { 0xb386, '約' },
        { 0xb387, '束' },
        { 0xb388, '恥' },
        { 0xb389, '辞' },
        { 0xb38a, '収' },
        { 0xb38b, '泉' },
        { 0xb38c, '浴' },
        { 0xb38d, '湯' },
        { 0xb38e, '僧' },
        { 0xb38f, '繰' },
        { 0xb390, '汗' },
        { 0xb391, '雨' },
        { 0xb392, '降' },
        { 0xb393, '酷' },
        { 0xb394, '肌' },
        { 0xb395, '潮' },
        { 0xb396, '晴' },
        { 0xb397, '枯' },
        { 0xb398, '乾' },
        { 0xb399, '恵' },
        { 0xb39a, '座' },
        { 0xb39b, '頂' },
        { 0xb39c, '将' },
        { 0xb39d, '責' },
        { 0xb39e, '任' },
        { 0xb39f, '系' },
        { 0xb3a0, '慢' },
        { 0xb3a1, '師' },
        { 0xb3a2, '拍' },
        { 0xb3a3, '検' },
        { 0xb3a4, '討' },
        { 0xb3a5, '越' },
        { 0xb3a6, '募' },
        { 0xb3a7, '標' },
        { 0xb3a8, '問' },
        { 0xb3a9, '題' },
        { 0xb3aa, '恋' },
        { 0xb3ab, '由' },
        { 0xb3ac, '泥' },
        { 0xb3ad, '撲' },
        { 0xb3ae, '替' },
        { 0xb3af, '射' },
        { 0xb3b0, '個' },
        { 0xb3b1, '補' },
        { 0xb3b2, '着' },
        { 0xb3b3, '芽' },
        { 0xb3b4, '樹' },
        { 0xb3b5, '科' },
        { 0xb3b6, '宇' },
        { 0xb3b7, '絵' },
        { 0xb3b8, '壇' },
        { 0xb3b9, '議' },
        { 0xb3ba, '舞' },
        { 0xb3bb, '遭' },
        { 0xb3bc, '遇' },
        { 0xb3bd, '雲' },
        { 0xb3be, '朝' },
        { 0xb3bf, '博' },
        { 0xb3c0, '途' },
        { 0xb3c1, '冒' },
        { 0xb3c2, '旅' },
        { 0xb3c3, '宝' },
        { 0xb3c4, '央' },
        { 0xb3c5, '脈' },
        { 0xb3c6, '答' },
        { 0xb3c7, '落' },
        { 0xb3c8, '惜' },
        { 0xb3c9, '雰' },
        { 0xb3ca, '治' },
        { 0xb3cb, '珍' },
        { 0xb3cc, '幸' },
        { 0xb3cd, '扱' },
        { 0xb3ce, '備' },
        { 0xb3cf, '輪' },
        { 0xb3d0, '凶' },
        { 0xb3d1, '旋' },
        { 0xb3d2, '脚' },
        { 0xb3d3, '呪' },
        { 0xb3d4, '爆' },
        { 0xb3d5, '裂' },
        { 0xb3d6, '掌' },
        { 0xb3d7, '居' },
        { 0xb3d8, '冥' },
        { 0xb3d9, '割' },
        { 0xb3da, '爪' },
        { 0xb3db, '拳' },
        { 0xb3dc, '屁' },
        { 0xb3dd, '刈' },
        { 0xb3de, '仙' },
        { 0xb3df, '酔' },
        { 0xb3e0, '跳' },
        { 0xb3e1, '津' },
        { 0xb3e2, '波' },
        { 0xb3e3, '八' },
        { 0xb3e4, '裏' },
        { 0xb3e5, '液' },
        { 0xb3e6, '逆' },
        { 0xb3e7, '芸' },
        { 0xb3e8, '馬' },
        { 0xb3e9, '挑' },
        { 0xb3ea, '吉' },
        { 0xb3eb, '析' },
        { 0xb3ec, '志' },
        { 0xb3ed, '閉' },
        { 0xb3ee, '悲' },
        { 0xb3ef, '滅' },
        { 0xb3f0, '複' },
        { 0xb3f1, '雑' },
        { 0xb3f2, '洞' },
        { 0xb3f3, '幻' },
        { 0xb3f4, '弱' },
        { 0xb3f5, '彼' },
        { 0xb3f6, '努' },
        { 0xb3f7, '魚' },
        { 0xb3f8, '毎' },
        { 0xb3f9, '極' },
        { 0xb3fa, '騎' },
        { 0xb3fb, '機' },
        { 0xb3fc, '邪' },
        { 0xb3fd, '普' },
        { 0xb3fe, '捕' },
        { 0xb3ff, '借' },
        { 0xb400, '葉' },
        { 0xb401, '奴' },
        { 0xb402, '鳥' },
        { 0xb403, '及' },
        { 0xb404, '異' },
        { 0xb405, '際' },
        { 0xb406, '驚' },
        { 0xb407, '浮' },
        { 0xb408, '描' },
        { 0xb409, '慈' },
        { 0xb40a, '掛' },
        { 0xb40b, '透' },
        { 0xb40c, '湖' },
        { 0xb40d, '敢' },
        { 0xb40e, '服' },
        { 0xb40f, '臆' },
        { 0xb410, '林' },
        { 0xb411, '軍' },
        { 0xb412, '千' },
        { 0xb413, '霊' },
        { 0xb414, '求' },
        { 0xb415, '昼' },
        { 0xb416, '涙' },
        { 0xb417, '悩' },
        { 0xb418, '沢' },
        { 0xb419, '奇' },
        { 0xb41a, '橋' },
        { 0xb41b, '富' },
        { 0xb41c, '和' },
        { 0xb41d, '余' },
        { 0xb41e, '裕' },
        { 0xb41f, '賊' },
        { 0xb420, '燃' },
        { 0xb421, '豊' },
        { 0xb422, '害' },
        { 0xb423, '維' },
        { 0xb424, '辺' },
        { 0xb425, '吉' },
        { 0xb426, '吸' },
        { 0xb427, '廃' },
        { 0xb428, '永' },
        { 0xb429, '巣' },
        { 0xb42a, '齢' },
        { 0xb42b, '継' },
        { 0xb42c, '端' },
        { 0xb42d, '制' },
        { 0xb42e, '改' },
        { 0xb42f, '済' },
        { 0xb430, '更' },
        { 0xb431, '曲' },
        { 0xb432, '継' },
        { 0xb433, '索' },
        { 0xb434, '計' },
        { 0xb435, '更' },
        { 0xb436, '枠' },
        { 0xb437, '起' },
        { 0xb500, 'A' },
        { 0xb501, 'B' },
        { 0xb502, 'C' },
        { 0xb503, 'D' },
        { 0xb504, 'E' },
        { 0xb505, 'F' },
        { 0xb506, 'G' },
        { 0xb507, 'H' },
        { 0xb508, 'I' },
        { 0xb509, 'J' },
        { 0xb50a, 'K' },
        { 0xb50b, 'L' },
        { 0xb50c, 'M' },
        { 0xb50d, 'N' },
        { 0xb50e, 'O' },
        { 0xb50f, 'P' },
        { 0xb510, 'Q' },
        { 0xb511, 'R' },
        { 0xb512, 'S' },
        { 0xb513, 'T' },
        { 0xb514, 'U' },
        { 0xb515, 'V' },
        { 0xb516, 'W' },
        { 0xb517, 'X' },
        { 0xb518, 'Y' },
        { 0xb519, 'Z' },
        { 0xb51a, 'a' },
        { 0xb51b, 'b' },
        { 0xb51c, 'c' },
        { 0xb51d, 'd' },
        { 0xb51e, 'e' },
        { 0xb51f, 'f' },
        { 0xb520, 'g' },
        { 0xb521, 'h' },
        { 0xb522, 'i' },
        { 0xb523, 'j' },
        { 0xb524, 'k' },
        { 0xb525, 'l' },
        { 0xb526, 'm' },
        { 0xb527, 'n' },
        { 0xb528, 'o' },
        { 0xb529, 'p' },
        { 0xb52a, 'q' },
        { 0xb52b, 'r' },
        { 0xb52c, 's' },
        { 0xb52d, 't' },
        { 0xb52e, 'u' },
        { 0xb52f, 'v' },
        { 0xb530, 'w' },
        { 0xb531, 'x' },
        { 0xb532, 'y' },
        { 0xb533, 'z' },
        { 0xb534, '.' },
        { 0xb535, '·' },
        { 0xb536, '!' },
        { 0xb537, '?' },
        { 0xb538, '—' },
        { 0xb539, ',' },
        { 0xb53a, '◦' },
        { 0xb53b, '+' },
        { 0xb53c, '@' },
        { 0xb53d, '/' },
        { 0xb53e, '-' },
        { 0xb53f, '%' },
        { 0xb540, '=' },
        { 0xb541, '⎡' },
        { 0xb542, '⎦' },
        { 0xb543, '○' },
        { 0xb544, '△' },
        { 0xb545, '□' },
        { 0xb546, '×' },
        { 0xb547, '▲' },
        { 0xb548, '▼' },
        { 0xb549, '\'' },
        { 0xb54a, '"' },
        { 0xb54b, ':' },
        { 0xb54c, '~' },
        { 0xb54d, '←' },
        { 0xb54e, '→' },
        { 0xb54f, '(' },
        { 0xb550, ')' },
        { 0xb551, '÷' },
        { 0xb552, '〒' },
        { 0xb553, '◎' },
        { 0xb554, '↑' },
        { 0xb555, '■' },
        { 0xb556, '↓' },
        { 0xb557, '0' },
        { 0xb558, '1' },
        { 0xb559, '2' },
        { 0xb55a, '3' },
        { 0xb55b, '4' },
        { 0xb55c, '5' },
        { 0xb55d, '6' },
        { 0xb55e, '7' },
        { 0xb55f, '8' },
        { 0xb560, '9' },
        { 0xb561, ' ' },
        { 0xff, '\0' },
        { 0xffff, '\0' }
    };

    public static readonly Dictionary<char, ushort> Reverse =
        Forward.GroupBy(x => x.Value).ToDictionary(x => x.Key, x => x.First().Key);
}

public static class Mr2StringExtension
{
    public static ushort[] AsMr2(this string text)
    {
        return text.Select(ch => CharMap.Reverse[ch]).Append<ushort>(0xffff).ToArray();
    }

    public static string AsString(this ushort[] text)
    {
        return new string(text.Select(ch => CharMap.Forward[ch]).ToArray()).Trim('\0');
    }

    public static ushort[] AsShorts(this byte[] raw)
    {
        var sdata = new ushort[raw.Length / 2];
        for (var j = 0; j < sdata.Length; ++j) sdata[j] = (ushort)((raw[j * 2] << 8) | raw[j * 2 + 1]);
        return sdata;
    }

    public static byte[] AsBytes(this ushort[] text)
    {
        var bytes = new byte[text.Length * 2];
        for (var j = 0; j < text.Length; ++j)
        {
            bytes[j * 2] = (byte)((text[j] & 0xff00) >> 8);
            bytes[j * 2 + 1] = (byte)(text[j] & 0xff);
        }

        return bytes;
    }
}