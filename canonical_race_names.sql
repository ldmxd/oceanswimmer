-- ============================================================
-- Migration: populate CanonicalRaceName on dbo.Race (full)
-- Covers all 1451 rows from races for canonical.xlsx
-- ============================================================

-- 3 Points Challenge and Ocean Swim (39 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'3 Points Challenge and Ocean Swim'
    WHERE raceid IN (25, 27, 28, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 665, 707, 747, 783, 864, 1203, 1278, 1365);

-- 5 Beaches Swim (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'5 Beaches Swim'
    WHERE raceid IN (22, 1283, 1477);

-- Across Lake Macquarie (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Across Lake Macquarie'
    WHERE raceid IN (966);

-- Across Newcastle Harbour (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Across Newcastle Harbour'
    WHERE raceid IN (952);

-- Australia Day Aquathon - Wollongong (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Australia Day Aquathon - Wollongong'
    WHERE raceid IN (590);

-- Australia Day Aquathon -Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Australia Day Aquathon -Swim'
    WHERE raceid IN (1116);

-- Australia Day Kids Aquathon -Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Australia Day Kids Aquathon -Swim'
    WHERE raceid IN (1118);

-- Australia Day Newcastle Harbour Swim (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Australia Day Newcastle Harbour Swim'
    WHERE raceid IN (89, 90);

-- Australia Day Short Aquathon -Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Australia Day Short Aquathon -Swim'
    WHERE raceid IN (1117);

-- Avalon (13 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avalon'
    WHERE raceid IN (568, 588, 632, 673, 714, 752, 790, 823, 876, 946, 1027, 1217, 1294);

-- Avalon 1.5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avalon 1.5km'
    WHERE raceid IN (1422);

-- Avalon 1km (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avalon 1km'
    WHERE raceid IN (1218, 1295, 1423);

-- Avalon Around the Bend (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avalon Around the Bend'
    WHERE raceid IN (1421);

-- Avoca (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avoca'
    WHERE raceid IN (584, 628, 668, 1133);

-- Avoca 500m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avoca 500m'
    WHERE raceid IN (1134);

-- Avoca Beach Open (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Avoca Beach Open'
    WHERE raceid IN (566);

-- Balina 850m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balina 850m'
    WHERE raceid IN (1131);

-- Balina Lakeside Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balina Lakeside Swim'
    WHERE raceid IN (1132);

-- Balmoral (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral'
    WHERE raceid IN (642, 688, 846, 899, 1055);

-- Balmoral 10km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral 10km'
    WHERE raceid IN (1210);

-- Balmoral 10km Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral 10km Swim'
    WHERE raceid IN (1094);

-- Balmoral 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral 1km'
    WHERE raceid IN (1408);

-- Balmoral 5km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral 5km'
    WHERE raceid IN (1209, 1407);

-- Balmoral 5km Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral 5km Swim'
    WHERE raceid IN (1095);

-- Balmoral Junior Swim (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral Junior Swim'
    WHERE raceid IN (961, 1151);

-- Balmoral Swim (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral Swim'
    WHERE raceid IN (410, 411, 412, 550, 551, 552, 962);

-- Balmoral Swim for CARE (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral Swim for CARE'
    WHERE raceid IN (729);

-- Balmoral Swim for Cancer (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral Swim for Cancer'
    WHERE raceid IN (771, 1146, 1235);

-- Balmoral- Correct Cat Results (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Balmoral- Correct Cat Results'
    WHERE raceid IN (615);

-- Barney Mullins Swim Classic (20 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Barney Mullins Swim Classic'
    WHERE raceid IN (385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 730, 842, 891, 974, 1044, 1141, 1234, 1312);

-- Bay to Boulders Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bay to Boulders Swim'
    WHERE raceid IN (1148);

-- Bay to Breakers Hawks Nest (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bay to Breakers Hawks Nest'
    WHERE raceid IN (755);

-- Bilgola (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bilgola'
    WHERE raceid IN (749, 819, 865, 931, 1045);

-- Bilgola Ocean Swim (30 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bilgola Ocean Swim'
    WHERE raceid IN (10, 11, 406, 407, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 565, 583, 625, 664, 701, 712, 750, 786, 1127, 1128, 1204, 1205, 1284, 1285, 1377, 1378);

-- Black Head (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Black Head'
    WHERE raceid IN (1296);

-- Black Head - Head 2 Head (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Black Head - Head 2 Head'
    WHERE raceid IN (1219);

-- Black Head 700m (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Black Head 700m'
    WHERE raceid IN (1220, 1297);

-- Blackhead (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Blackhead'
    WHERE raceid IN (870);

-- Blacksmiths (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Blacksmiths'
    WHERE raceid IN (1256);

-- Blacksmiths 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Blacksmiths 1km'
    WHERE raceid IN (1255);

-- Bondi Bluewater Challenge (56 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bondi Bluewater Challenge'
    WHERE raceid IN (480, 481, 482, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 852, 853, 905, 906, 972, 973, 1042, 1043, 1135, 1136, 1137, 1138, 1230, 1231, 1232, 1233, 1338, 1339, 1404);
UPDATE dbo.Race SET CanonicalRaceName = N'Bondi Bluewater Challenge'
    WHERE raceid IN (1405, 1472, 1473, 1474, 1475, 1476);

-- Bondi Splash Series 1km Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bondi Splash Series 1km Swim'
    WHERE raceid IN (1364);

-- Bondi to Bronte (67 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bondi to Bronte'
    WHERE raceid IN (23, 24, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 320, 321, 564, 582, 622, 661);
UPDATE dbo.Race SET CanonicalRaceName = N'Bondi to Bronte'
    WHERE raceid IN (700, 748, 784, 818, 930, 1014, 1090, 1091, 1092, 1093, 1202, 1279, 1280, 1281, 1282, 1366, 1367);

-- Bridge to Beach (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Bridge to Beach'
    WHERE raceid IN (804);

-- Brisbane Water 1Km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Brisbane Water 1Km'
    WHERE raceid IN (644);

-- Brisbane Water 3Km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Brisbane Water 3Km'
    WHERE raceid IN (645);

-- Brisbane Water 5Km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Brisbane Water 5Km'
    WHERE raceid IN (646);

-- Broulee Bay to Breakers (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Broulee Bay to Breakers'
    WHERE raceid IN (892);

-- Broulee Bay to Breakers Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Broulee Bay to Breakers Ocean Swim'
    WHERE raceid IN (960);

-- Busselton Jetty Swim (9 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Busselton Jetty Swim'
    WHERE raceid IN (440, 441, 442, 443, 444, 445, 446, 447, 448);

-- Byron Bay (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Byron Bay'
    WHERE raceid IN (1258, 1343);

-- Byron Bay Mini (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Byron Bay Mini'
    WHERE raceid IN (1259, 1342);

-- Byron Bay Ocean Swim Classic (29 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Byron Bay Ocean Swim Classic'
    WHERE raceid IN (478, 479, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549);

-- Byron Bay Winter Whales (6 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Byron Bay Winter Whales'
    WHERE raceid IN (607, 654, 696, 740, 777, 1066);

-- Captain Christie Classic (23 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Captain Christie Classic'
    WHERE raceid IN (13, 485, 486, 487, 488, 489, 491, 587, 629, 630, 631, 636, 672, 736, 798, 830, 875, 941, 1024, 1098, 1212, 1293, 1379);

-- Caves Beach (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Caves Beach'
    WHERE raceid IN (648, 680, 720, 767, 902, 1142, 1337);

-- Caves Beach - Newcastle (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Caves Beach - Newcastle'
    WHERE raceid IN (597);

-- Caves Beach Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Caves Beach Ocean Swim'
    WHERE raceid IN (959);

-- Chieftain Challenge (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Chieftain Challenge'
    WHERE raceid IN (21);

-- Clarence River (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Clarence River'
    WHERE raceid IN (702);

-- Clearwater Classic Jervis Bay (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Clearwater Classic Jervis Bay'
    WHERE raceid IN (1143);

-- Club to Club Foster (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Club to Club Foster'
    WHERE raceid IN (769);

-- Coffs 150m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs 150m'
    WHERE raceid IN (1162);

-- Coffs 300m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs 300m'
    WHERE raceid IN (1161);

-- Coffs 600m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs 600m'
    WHERE raceid IN (1160);

-- Coffs Coast (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs Coast'
    WHERE raceid IN (595);

-- Coffs Coast Swin (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs Coast Swin'
    WHERE raceid IN (637);

-- Coffs Harbour (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coffs Harbour'
    WHERE raceid IN (682, 723, 803, 1159);

-- Cole Classic (75 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Cole Classic'
    WHERE raceid IN (51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 322, 323, 324, 571, 612, 634, 676, 677, 717, 718, 735, 758, 759, 795);
UPDATE dbo.Race SET CanonicalRaceName = N'Cole Classic'
    WHERE raceid IN (796, 826, 827, 879, 880, 881, 882, 883, 884, 1119, 1120, 1121, 1122, 1123, 1124, 1305, 1306, 1307, 1393, 1394, 1395, 1396, 1397, 1398, 1399);

-- Collaroy Ocean Swim (18 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Collaroy Ocean Swim'
    WHERE raceid IN (41, 42, 429, 432, 433, 434, 435, 436, 856, 1000, 1074, 1075, 1189, 1190, 1265, 1266, 1352, 1353);

-- Coogee (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee'
    WHERE raceid IN (1418);

-- Coogee - Wedding Cake Island Swim (58 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee - Wedding Cake Island Swim'
    WHERE raceid IN (29, 30, 31, 32, 207, 208, 209, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 656, 697, 745);
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee - Wedding Cake Island Swim'
    WHERE raceid IN (782, 815, 859, 925, 1008, 1009, 1201, 1363);

-- Coogee 1km (12 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee 1km'
    WHERE raceid IN (816, 860, 911, 926, 1083, 1171, 1200, 1252, 1274, 1318, 1362, 1419);

-- Coogee 1km v2 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee 1km v2'
    WHERE raceid IN (985);

-- Coogee 800m (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee 800m'
    WHERE raceid IN (986, 1082, 1172, 1361, 1420);

-- Coogee 800m Jnrs (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee 800m Jnrs'
    WHERE raceid IN (1275);

-- Coogee Island (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Island'
    WHERE raceid IN (1317);

-- Coogee Island Challenge (29 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Island Challenge'
    WHERE raceid IN (192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 210, 211, 212, 213, 214, 215, 437, 438, 439, 912, 1084, 1170, 1253, 1273);

-- Coogee Island v2 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Island v2'
    WHERE raceid IN (984);

-- Coogee Jnr 800m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Jnr 800m'
    WHERE raceid IN (1251);

-- Coogee Junior (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Junior'
    WHERE raceid IN (861, 1319);

-- Coogee Junior 600m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Junior 600m'
    WHERE raceid IN (1199);

-- Coogee Junior Dash (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Junior Dash'
    WHERE raceid IN (817);

-- Coogee Splash (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Splash'
    WHERE raceid IN (621, 660, 746);

-- Coogee Splash and Dash (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee Splash and Dash'
    WHERE raceid IN (610, 698, 781);

-- Coogee to Bondi (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Coogee to Bondi'
    WHERE raceid IN (776, 848, 914, 1368);

-- Cook Community Classic (14 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Cook Community Classic'
    WHERE raceid IN (39, 40, 811, 812, 919, 920, 1001, 1002, 1191, 1192, 1267, 1268, 1354, 1355);

-- Copeton 600m (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton 600m'
    WHERE raceid IN (990, 1064, 1177);

-- Copeton Freshwater (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton Freshwater'
    WHERE raceid IN (989);

-- Copeton Freshwater 2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton Freshwater 2km'
    WHERE raceid IN (1063);

-- Copeton Freshwater 5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton Freshwater 5km'
    WHERE raceid IN (1062);

-- Copeton Waters (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton Waters'
    WHERE raceid IN (1175);

-- Copeton Waters 2.5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Copeton Waters 2.5km'
    WHERE raceid IN (1176);

-- DY2K (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'DY2K'
    WHERE raceid IN (683, 721);

-- DY2K - Dee Why (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'DY2K - Dee Why'
    WHERE raceid IN (592);

-- DY2k - Dee Why (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'DY2k - Dee Why'
    WHERE raceid IN (573);

-- Dawny's Cockatoo Challenge (29 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Dawny''s Cockatoo Challenge'
    WHERE raceid IN (37, 38, 579, 611, 616, 667, 699, 704, 741, 742, 743, 779, 780, 813, 814, 857, 858, 921, 922, 1003, 1004, 1087, 1088, 1193, 1194, 1276, 1277, 1356, 1357);

-- Dee Why (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Dee Why'
    WHERE raceid IN (895);

-- Dee Why 750 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Dee Why 750'
    WHERE raceid IN (896);

-- Deewhy 2K Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Deewhy 2K Swim'
    WHERE raceid IN (640);

-- Definition Health Clubs -Nth Curl Curl (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Definition Health Clubs -Nth Curl Curl'
    WHERE raceid IN (623);

-- Express Glass Island Challenge (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Express Glass Island Challenge'
    WHERE raceid IN (563, 580);

-- Express Glass Island Challenge - Coogee (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Express Glass Island Challenge - Coogee'
    WHERE raceid IN (620);

-- Fingal Bay Half Mile (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Fingal Bay Half Mile'
    WHERE raceid IN (1153);

-- Fingal Bay Mile Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Fingal Bay Mile Swim'
    WHERE raceid IN (1152);

-- Fingal Half Mile (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Fingal Half Mile'
    WHERE raceid IN (1029);

-- Fingal Mile (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Fingal Mile'
    WHERE raceid IN (1028);

-- Forresters 1.5Km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters 1.5Km'
    WHERE raceid IN (1261);

-- Forresters 1.5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters 1.5km'
    WHERE raceid IN (1186);

-- Forresters Beach (6 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach'
    WHERE raceid IN (703, 797, 834, 995, 1067, 1185);

-- Forresters Beach 1.5 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach 1.5'
    WHERE raceid IN (996);

-- Forresters Beach 1.5km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach 1.5km'
    WHERE raceid IN (923, 1068);

-- Forresters Beach 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach 1km'
    WHERE raceid IN (833);

-- Forresters Beach 3km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach 3km'
    WHERE raceid IN (924);

-- Forresters Beach Jnrs (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Beach Jnrs'
    WHERE raceid IN (997);

-- Forresters Island Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forresters Island Ocean Swim'
    WHERE raceid IN (1260);

-- Forster (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forster'
    WHERE raceid IN (1321);

-- Forster 1km (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forster 1km'
    WHERE raceid IN (982, 1056, 1157, 1322);

-- Forster 250m (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forster 250m'
    WHERE raceid IN (983, 1057, 1158, 1323);

-- Forster Club to Club (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Forster Club to Club'
    WHERE raceid IN (695, 910, 981, 1156, 1426);

-- Foster (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Foster'
    WHERE raceid IN (653);

-- Foster 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Foster 1km'
    WHERE raceid IN (1246);

-- Foster Club to Club (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Foster Club to Club'
    WHERE raceid IN (1245);

-- Freshwater (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Freshwater'
    WHERE raceid IN (802, 1406);

-- Gaol Break Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gaol Break Ocean Swim'
    WHERE raceid IN (807);

-- Gaol Break South West Rocks (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gaol Break South West Rocks'
    WHERE raceid IN (737);

-- Gaol Break Sth West Rocks (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gaol Break Sth West Rocks'
    WHERE raceid IN (1046);

-- Gaol Break Sth Wst Rocks (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gaol Break Sth Wst Rocks'
    WHERE raceid IN (689);

-- Gaol Break Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gaol Break Swim'
    WHERE raceid IN (600);

-- Gosford Brisbane Water (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gosford Brisbane Water'
    WHERE raceid IN (686);

-- Gosford Stingrays - Terrigal 1km (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gosford Stingrays - Terrigal 1km'
    WHERE raceid IN (608, 617, 709);

-- Gosford Stingrays - Terrigal 3km (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gosford Stingrays - Terrigal 3km'
    WHERE raceid IN (609, 618, 710);

-- Gosford Stingrays - Terrigal 5km (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gosford Stingrays - Terrigal 5km'
    WHERE raceid IN (578, 619, 711);

-- Gosford Stingrays- Terrigal (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Gosford Stingrays- Terrigal'
    WHERE raceid IN (601);

-- Head2Head (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Head2Head'
    WHERE raceid IN (943, 1178);

-- Head2Head 700m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Head2Head 700m'
    WHERE raceid IN (942);

-- Husky Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Husky Ocean Swim'
    WHERE raceid IN (1140);

-- Ice Cold Classic (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Ice Cold Classic'
    WHERE raceid IN (423);

-- Jervis Bay 1.5 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay 1.5'
    WHERE raceid IN (1346);

-- Jervis Bay 1.9 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay 1.9'
    WHERE raceid IN (1345);

-- Jervis Bay 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay 1km'
    WHERE raceid IN (1144);

-- Jervis Bay 3.8 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay 3.8'
    WHERE raceid IN (1344);

-- Jervis Bay 750m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay 750m'
    WHERE raceid IN (1347);

-- Jervis Bay Mini Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jervis Bay Mini Swim'
    WHERE raceid IN (1145);

-- Jim Kerry - Nth Wollongong (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Jim Kerry - Nth Wollongong'
    WHERE raceid IN (785);

-- Lake Macquarie (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Lake Macquarie'
    WHERE raceid IN (1051);

-- Lake Macquarie 1km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Lake Macquarie 1km'
    WHERE raceid IN (967, 1052);

-- Lake Macquarie Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Lake Macquarie Swim'
    WHERE raceid IN (889);

-- Long Reef (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Long Reef'
    WHERE raceid IN (626, 666, 708);

-- Long Reef 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Long Reef 1km'
    WHERE raceid IN (839);

-- Long Reef 2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Long Reef 2km'
    WHERE raceid IN (838);

-- Malabar to Little Bay (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Malabar to Little Bay'
    WHERE raceid IN (762, 799);

-- Manly (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly'
    WHERE raceid IN (722);

-- Manly 1 km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly 1 km'
    WHERE raceid IN (726);

-- Manly 1km (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly 1km'
    WHERE raceid IN (684, 801, 1019);

-- Manly 1km Blue Dolphins (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly 1km Blue Dolphins'
    WHERE raceid IN (867);

-- Manly 2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly 2km'
    WHERE raceid IN (837);

-- Manly Blue Dolphins (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Blue Dolphins'
    WHERE raceid IN (866, 1018);

-- Manly Blue Dolphins 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Blue Dolphins 1km'
    WHERE raceid IN (934);

-- Manly Blue Dolphins Ocean Swiim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Blue Dolphins Ocean Swiim'
    WHERE raceid IN (933);

-- Manly Daily Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Daily Swim'
    WHERE raceid IN (800);

-- Manly Steyne Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Steyne Swim'
    WHERE raceid IN (638);

-- Manly Warf 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Warf 1km'
    WHERE raceid IN (766);

-- Manly Warf Hotel (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Warf Hotel'
    WHERE raceid IN (681);

-- Manly Wharf Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly Wharf Swim'
    WHERE raceid IN (765);

-- Manly1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Manly1km'
    WHERE raceid IN (836);

-- Mollymook (8 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mollymook'
    WHERE raceid IN (652, 694, 774, 909, 1173, 1248, 1315, 1416);

-- Mollymook 500m (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mollymook 500m'
    WHERE raceid IN (1174, 1247, 1316, 1417);

-- Mollymook Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mollymook Ocean Swim'
    WHERE raceid IN (606);

-- Mollymook Ocean Swim Classic (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mollymook Ocean Swim Classic'
    WHERE raceid IN (739);

-- Mollymook Ocean Swims (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mollymook Ocean Swims'
    WHERE raceid IN (381, 382, 383, 384);

-- Mona Vale Cold Water (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mona Vale Cold Water'
    WHERE raceid IN (778, 994);

-- Mona Vale Hospital Jubilee (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mona Vale Hospital Jubilee'
    WHERE raceid IN (1041);

-- Mona Vale Ocean Swims (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mona Vale Ocean Swims'
    WHERE raceid IN (361, 362);

-- Mona Vale Swims (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Mona Vale Swims'
    WHERE raceid IN (408, 409);

-- Monavale Cold Water (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Monavale Cold Water'
    WHERE raceid IN (854);

-- Murray Rose Malabar Magic (32 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Murray Rose Malabar Magic'
    WHERE raceid IN (47, 48, 831, 832, 887, 888, 957, 958, 1039, 1040, 1129, 1130, 1228, 1229, 1310, 1311, 1402, 1403, 1427, 1428, 1429, 1430, 1432, 1433, 1434, 1435, 1436, 1437, 1438, 1439, 1440, 1441);

-- Narrabeen Challenge (20 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Narrabeen Challenge'
    WHERE raceid IN (43, 44, 449, 455, 809, 855, 918, 998, 999, 1069, 1070, 1187, 1188, 1262, 1263, 1264, 1348, 1349, 1350, 1351);

-- Newcastle 700m (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newcastle 700m'
    WHERE raceid IN (951, 1032, 1222);

-- Newcastle Harbour (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newcastle Harbour'
    WHERE raceid IN (1031, 1223);

-- Newcastle Harbour Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newcastle Harbour Swim'
    WHERE raceid IN (678);

-- Newcastle Harbour Swim Classic (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newcastle Harbour Swim Classic'
    WHERE raceid IN (6, 7);

-- Newport (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newport'
    WHERE raceid IN (572, 787, 871, 940);

-- Newport 800 (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newport 800'
    WHERE raceid IN (872, 1022);

-- Newport 800m (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newport 800m'
    WHERE raceid IN (939, 1100);

-- Newport Pool to Peak (49 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newport Pool to Peak'
    WHERE raceid IN (16, 17, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 599, 679, 724, 751, 820, 1023, 1099, 1211, 1289, 1290, 1372, 1373, 1374, 1375, 1376);

-- Newport to Avalon (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Newport to Avalon'
    WHERE raceid IN (1320);

-- Nobbys Newcastle (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nobbys Newcastle'
    WHERE raceid IN (1139, 1299);

-- Nobbys To Newcastle (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nobbys To Newcastle'
    WHERE raceid IN (1206);

-- Nobbys to Newcastle (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nobbys to Newcastle'
    WHERE raceid IN (932);

-- Nobbys-Newcastle (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nobbys-Newcastle'
    WHERE raceid IN (1017);

-- Nobbys-Newcastle Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nobbys-Newcastle Swim'
    WHERE raceid IN (5);

-- North Bondi - Rough Water Swim (24 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi - Rough Water Swim'
    WHERE raceid IN (713, 788, 789, 821, 944, 945, 1026, 1215, 1216, 1456, 1457, 1458, 1459, 1460, 1461, 1462, 1463, 1464, 1465, 1466, 1468, 1469, 1470, 1471);

-- North Bondi 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi 1km'
    WHERE raceid IN (1309);

-- North Bondi Classic (54 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi Classic'
    WHERE raceid IN (216, 217, 574, 575, 591, 593, 635, 719, 725, 760, 761, 793, 794, 822, 828, 829, 885, 886, 955, 956, 1025, 1035, 1036, 1101, 1102, 1103, 1104, 1105, 1106, 1107, 1125, 1126, 1226, 1227, 1291, 1292, 1308, 1400, 1401, 1442, 1443, 1444, 1445, 1446, 1447, 1448, 1449, 1450, 1451, 1452);
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi Classic'
    WHERE raceid IN (1453, 1454, 1455, 1467);

-- North Bondi Roughwater (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi Roughwater'
    WHERE raceid IN (586, 670, 873);

-- North Bondi Roughwater 2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Bondi Roughwater 2km'
    WHERE raceid IN (754);

-- North Curl Curl (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Curl Curl'
    WHERE raceid IN (1089, 1329);

-- North Curl Curl 1.5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Curl Curl 1.5km'
    WHERE raceid IN (1330);

-- North Steyne Ocean Swim (29 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Steyne Ocean Swim'
    WHERE raceid IN (397, 398, 399, 400, 401, 402, 403, 404, 577, 594, 647, 690, 691, 733, 734, 900, 901, 969, 970, 1053, 1054, 1154, 1155, 1249, 1250, 1313, 1314, 1409, 1410);

-- North Woolongong (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'North Woolongong'
    WHERE raceid IN (641);

-- Nth Avoca 500m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nth Avoca 500m'
    WHERE raceid IN (1016);

-- Nth Avoca Beach Fest (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nth Avoca Beach Fest'
    WHERE raceid IN (1015);

-- Nth Curl Curl (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Nth Curl Curl'
    WHERE raceid IN (927);

-- Pacific Palms (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Pacific Palms'
    WHERE raceid IN (692, 904, 1061, 1327, 1414);

-- Pacific Palms - Rock to Rock (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Pacific Palms - Rock to Rock'
    WHERE raceid IN (849);

-- Pacific Palms 1.5 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Pacific Palms 1.5'
    WHERE raceid IN (1166);

-- Pacific Palms 600m (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Pacific Palms 600m'
    WHERE raceid IN (1167, 1242, 1328, 1415);

-- Palm Beach SLSA Enduro Champs (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Palm Beach SLSA Enduro Champs'
    WHERE raceid IN (810);

-- Pier to Pub (6 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Pier to Pub'
    WHERE raceid IN (45, 46, 91, 92, 1497, 1498);

-- Proclamation Classic (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Proclamation Classic'
    WHERE raceid IN (18, 19, 20);

-- Queenscliff (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Queenscliff'
    WHERE raceid IN (847, 1207, 1286);

-- Queenscliff 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Queenscliff 1km'
    WHERE raceid IN (1287);

-- Queenscliff 800m (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Queenscliff 800m'
    WHERE raceid IN (979, 1037, 1208);

-- Queenscliff Swim for Saxon (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Queenscliff Swim for Saxon'
    WHERE raceid IN (1038);

-- Rock to Rock - Pacific Palms (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Rock to Rock - Pacific Palms'
    WHERE raceid IN (602, 731, 772);

-- Rock to Rock -Pacific Palms (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Rock to Rock -Pacific Palms'
    WHERE raceid IN (650);

-- Rock to Rock Pacific Palms (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Rock to Rock Pacific Palms'
    WHERE raceid IN (975, 1243);

-- Roughwater 1km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Roughwater 1km'
    WHERE raceid IN (753, 874);

-- Sandon Point (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sandon Point'
    WHERE raceid IN (971);

-- Shark Island Swim Classic (19 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shark Island Swim Classic'
    WHERE raceid IN (1, 2, 576, 598, 643, 844, 845, 897, 898, 991, 992, 1049, 1050, 1080, 1081, 1163, 1164, 1388, 1389);

-- Shell Harbour (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shell Harbour'
    WHERE raceid IN (651);

-- Shellharbour (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shellharbour'
    WHERE raceid IN (908, 1169, 1254, 1324, 1424);

-- Shellharbour 400m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shellharbour 400m'
    WHERE raceid IN (977);

-- Shellharbour Junior (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shellharbour Junior'
    WHERE raceid IN (1168, 1425);

-- Shellharbour Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Shellharbour Ocean Swim'
    WHERE raceid IN (978);

-- South Curl Curl (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Curl Curl'
    WHERE raceid IN (567, 585, 775, 851, 917, 1179, 1257);

-- South Curl Curl Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Curl Curl Swim'
    WHERE raceid IN (477);

-- South Curl Curl to Freshwater (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Curl Curl to Freshwater'
    WHERE raceid IN (1065, 1340);

-- South Head Roughwater (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Head Roughwater'
    WHERE raceid IN (605, 1182);

-- South Head Roughwater Duo (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Head Roughwater Duo'
    WHERE raceid IN (1183);

-- South Head Roughwater Teams (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Head Roughwater Teams'
    WHERE raceid IN (1184);

-- South Maroubra Ocean Swim (8 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South Maroubra Ocean Swim'
    WHERE raceid IN (457, 458, 459, 1303, 1304, 1390, 1391, 1392);

-- South West Rocks (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South West Rocks'
    WHERE raceid IN (963, 1236, 1334);

-- South West Rocks 200m (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South West Rocks 200m'
    WHERE raceid IN (1150, 1336);

-- South West Rocks 250m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South West Rocks 250m'
    WHERE raceid IN (1238);

-- South West Rocks 700m (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'South West Rocks 700m'
    WHERE raceid IN (1149, 1237, 1335);

-- Stanwell Park (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Stanwell Park'
    WHERE raceid IN (649, 687, 732, 770, 843, 903, 968);

-- Stanwell Park Challenge (3 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Stanwell Park Challenge'
    WHERE raceid IN (603, 1147, 1244);

-- Sth Curl Curl (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sth Curl Curl'
    WHERE raceid IN (671);

-- Sth Curl Curl to Freshwater (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sth Curl Curl to Freshwater'
    WHERE raceid IN (993);

-- Sth West Rocks 200 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sth West Rocks 200'
    WHERE raceid IN (1048);

-- Sth West Rocks 700 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sth West Rocks 700'
    WHERE raceid IN (1047);

-- Sun Run (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sun Run'
    WHERE raceid IN (87, 88);

-- Swim for Saxon (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Swim for Saxon'
    WHERE raceid IN (913, 980);

-- Sydney Harbour 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sydney Harbour 1km'
    WHERE raceid IN (764);

-- Sydney Harbour Splash (35 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sydney Harbour Splash'
    WHERE raceid IN (464, 465, 466, 467, 468, 469, 470, 471, 472, 1331, 1332, 1333, 1380, 1381, 1384, 1385, 1478, 1479, 1480, 1481, 1482, 1483, 1484, 1485, 1486, 1487, 1488, 1489, 1490, 1491, 1492, 1493, 1494, 1495, 1496);

-- Sydney Harbour Swim Classic (21 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Sydney Harbour Swim Classic'
    WHERE raceid IN (473, 474, 475, 476, 596, 639, 685, 727, 728, 763, 805, 806, 840, 841, 893, 894, 949, 950, 1113, 1114, 1115);

-- Tama2Cloey (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tama2Cloey'
    WHERE raceid IN (890);

-- TamaCloey Cliff-Side Odyssey (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'TamaCloey Cliff-Side Odyssey'
    WHERE raceid IN (808);

-- Tamarama 2 Clovelly (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tamarama 2 Clovelly'
    WHERE raceid IN (773);

-- Tamarama to Clovelly (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tamarama to Clovelly'
    WHERE raceid IN (835);

-- Tathra (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tathra'
    WHERE raceid IN (614);

-- Tathra Warf 2 Waves (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tathra Warf 2 Waves'
    WHERE raceid IN (1108);

-- Terrigal (6 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal'
    WHERE raceid IN (604, 915, 916, 988, 1059, 1325);

-- Terrigal 1km (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal 1km'
    WHERE raceid IN (657, 987, 1060, 1180, 1240, 1326, 1413);

-- Terrigal 2km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal 2km'
    WHERE raceid IN (1181, 1412);

-- Terrigal 3km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal 3km'
    WHERE raceid IN (658);

-- Terrigal 5km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal 5km'
    WHERE raceid IN (659);

-- Terrigal Classic (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal Classic'
    WHERE raceid IN (693);

-- Terrigal Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal Ocean Swim'
    WHERE raceid IN (1241);

-- Terrigal Ocean Swim Classic (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal Ocean Swim Classic'
    WHERE raceid IN (424, 425, 538, 539);

-- Terrigal Oceanswim (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal Oceanswim'
    WHERE raceid IN (426, 427);

-- Terrigal Oxigen 2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Terrigal Oxigen 2km'
    WHERE raceid IN (738);

-- The Aussies Ocean Swim (10 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Aussies Ocean Swim'
    WHERE raceid IN (413, 414, 415, 416, 417, 418, 419, 420, 421, 422);

-- The Big Swim (51 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Big Swim'
    WHERE raceid IN (3, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 675, 716, 757, 792, 825, 878, 953, 954, 1033, 1034, 1111, 1112, 1224, 1225, 1300, 1301, 1386);
UPDATE dbo.Race SET CanonicalRaceName = N'The Big Swim'
    WHERE raceid IN (1387);

-- The Big Swim - Palm to Whale (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Big Swim - Palm to Whale'
    WHERE raceid IN (570);

-- The Fingal Mile (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Fingal Mile'
    WHERE raceid IN (948);

-- The Little Big Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Little Big Swim'
    WHERE raceid IN (4);

-- The Roughwater (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'The Roughwater'
    WHERE raceid IN (12);

-- Tilbury Challenge (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tilbury Challenge'
    WHERE raceid IN (655);

-- Tilbury Classic (6 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tilbury Classic'
    WHERE raceid IN (907, 976, 1058, 1165, 1239, 1411);

-- Toowoon Bay Ocean Swim (29 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Toowoon Bay Ocean Swim'
    WHERE raceid IN (33, 34, 35, 36, 405, 624, 662, 705, 744, 928, 929, 1005, 1006, 1007, 1076, 1077, 1078, 1079, 1195, 1196, 1197, 1198, 1269, 1270, 1271, 1272, 1358, 1359, 1360);

-- True Mile Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'True Mile Swim'
    WHERE raceid IN (26);

-- Tweed 1.2km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tweed 1.2km'
    WHERE raceid IN (1086);

-- Tweed River (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tweed River'
    WHERE raceid IN (663, 706, 862, 1085);

-- Tweed River - Murwillumbah (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tweed River - Murwillumbah'
    WHERE raceid IN (581);

-- Tweed River 1km (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Tweed River 1km'
    WHERE raceid IN (863);

-- Vladswim Challenge (33 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Vladswim Challenge'
    WHERE raceid IN (94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 935, 936, 1369, 1370, 1371);

-- WA Open Water Swimming Series - Race 2 (4 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'WA Open Water Swimming Series - Race 2'
    WHERE raceid IN (460, 461, 462, 463);

-- Warf to Waves (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Warf to Waves'
    WHERE raceid IN (768);

-- Warriewood - Mona Vale Ocean Swim (43 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Warriewood - Mona Vale Ocean Swim'
    WHERE raceid IN (8, 9, 325, 358, 359, 360, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 569, 589, 633, 674, 715, 756, 791, 824, 850, 877, 947, 1030, 1109, 1110, 1221, 1298, 1341, 1382, 1383);

-- Wollongong (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Wollongong'
    WHERE raceid IN (964);

-- Wollongong 800m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Wollongong 800m'
    WHERE raceid IN (965);

-- Woy Woy (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Woy Woy'
    WHERE raceid IN (1071);

-- Woy Woy 1km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Woy Woy 1km'
    WHERE raceid IN (1012, 1073);

-- Woy Woy 2km (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Woy Woy 2km'
    WHERE raceid IN (1011, 1072);

-- Woy Woy 400m (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Woy Woy 400m'
    WHERE raceid IN (1013);

-- Woy Woy Marathon (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Woy Woy Marathon'
    WHERE raceid IN (1010);

-- Yamba (7 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba'
    WHERE raceid IN (627, 669, 868, 937, 1096, 1214, 1288);

-- Yamba 700 (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba 700'
    WHERE raceid IN (869);

-- Yamba 700m (5 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba 700m'
    WHERE raceid IN (938, 1020, 1097, 1213, 1302);

-- Yamba Convent to Main Beach Classic (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba Convent to Main Beach Classic'
    WHERE raceid IN (613);

-- Yamba Ocean Swim (1 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba Ocean Swim'
    WHERE raceid IN (1021);

-- Yamba Ocean Swims (2 rows)
UPDATE dbo.Race SET CanonicalRaceName = N'Yamba Ocean Swims'
    WHERE raceid IN (14, 15);

-- Total: 1451 rows, 295 distinct canonical names