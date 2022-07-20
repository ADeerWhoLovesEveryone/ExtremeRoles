﻿using System;
using System.Security.Cryptography;

using ExtremeRoles.Module.PRNG;

namespace ExtremeRoles
{
    public static class RandomGenerator
    {
        public static RNGBase Instance;
        public static bool prevValue = false;
        public static int prevSelection = 0;

        public static void Initialize()
        {
            bool useStrongGen = OptionHolder.AllOption[
                (int)OptionHolder.CommonOptionKey.UseStrongRandomGen].GetValue();
            if (Instance != null)
            {
                if (useStrongGen != prevValue)
                {
                    createGlobalRandomGenerator(useStrongGen);
                }
                else
                {
                    int selection = OptionHolder.AllOption[
                        (int)OptionHolder.CommonOptionKey.UsePrngAlgorithm].GetValue();
                    if (prevSelection != selection)
                    {
                        Instance = getAditionalPrng(selection);
                        UnityEngine.Random.InitState(CreateStrongRandomSeed());
                        prevSelection = selection;
                    }
                }
                Instance.Next();
            }
            else
            {
                createGlobalRandomGenerator(useStrongGen);
            }

            Helper.Logging.Debug($"UsePRNG:{Instance}");

        }

        private static void createGlobalRandomGenerator(bool isStrong)
        {

            if (isStrong)
            {
                int selection = OptionHolder.AllOption[
                    (int)OptionHolder.CommonOptionKey.UsePrngAlgorithm].GetValue();
                Instance = getAditionalPrng(selection);
                UnityEngine.Random.InitState(CreateStrongRandomSeed());
                prevSelection = selection;
            }
            else
            {
                Instance = new SystemRandomWrapper(0, 0);
                UnityEngine.Random.InitState(createNormalRandomSeed());
                prevSelection = -1;
            }
            prevValue = isStrong;
        }

        public static Random GetTempGenerator()
        {
            bool useStrongGen = OptionHolder.AllOption[
                (int)OptionHolder.CommonOptionKey.UseStrongRandomGen].GetValue();

            if (useStrongGen)
            {
                return new Random(CreateStrongRandomSeed());
            }
            else
            {
                return new Random(createNormalRandomSeed());
            }
        }

        public static int CreateStrongRandomSeed()
        {
            var bs = new byte[4];
            //Int32と同じサイズのバイト配列にランダムな値を設定する
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bs);
            }

            Helper.Logging.Debug($"Int32 SeedValue:{string.Join("", bs)}");

            //RNGCryptoServiceProviderで得たbit列をInt32型に変換してシード値とする。
            return BitConverter.ToInt32(bs, 0);
        }

        public static ulong CreateLongStrongSeed()
        {
            var bs = new byte[8];
            //Int64と同じサイズのバイト配列にランダムな値を設定する
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bs);
            }

            Helper.Logging.Debug($"UInt64 Seed:{string.Join("", bs)}");

            //RNGCryptoServiceProviderで得たbit列をUInt64型に変換してシード値とする。
            return BitConverter.ToUInt64(bs, 0);
        }

        private static int createNormalRandomSeed()
        {
            return ((int)DateTime.Now.Ticks & 0x0000FFFF) + UnityEngine.SystemInfo.processorFrequency;
        }

        private static RNGBase getAditionalPrng(int selection)
        {
            switch (selection)
            {
                case 0:
                    return new Pcg32XshRr(
                        CreateLongStrongSeed(),
                        CreateLongStrongSeed());
                case 1:
                    return new Pcg64RxsMXs(
                        CreateLongStrongSeed(),
                        CreateLongStrongSeed());
                case 2:
                    return new Xorshiro256StarStar(
                        CreateLongStrongSeed(),
                        CreateLongStrongSeed());
                case 3:
                    return new Xorshiro512StarStar(
                        CreateLongStrongSeed(),
                        CreateLongStrongSeed());
                default:
                    return new SystemRandomWrapper(0, 0);
            }
        }

    }
}
