using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PIE.Meteo.FileProject
{
    public static class MemoryHelper
    {
        /// <summary>
        /// 可用物理内存
        /// </summary>
        /// <returns></returns>
        public static ulong GetAvalidPhyMemory()
        {
            return Convert.ToUInt64(GC.GetTotalMemory(false) / 2);
        }

        public static long WorkingSet64()
        {
            Process process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }

        /// <summary>
        /// 整幅投影对内存做限制，系统剩余内存不低于A参数MB，应用程序已使用内存不超过B参数MB
        /// 不满足条件会引发异常，参数0表示需要的可用内存大小，参数2表示进程使用的内存大小
        /// </summary>
        /// <param name="avalidPhyMemoryMin">（当前操作系统）剩余内存限制（MB）</param>
        /// <param name="usedMemMax">（当前应用程序）已使用内存限制（MB）</param>
        public static void MemoryNeed(int avalidPhyMemoryMin, int usedMemMax)
        {
            ulong avalidPhyMemory = MemoryHelper.GetAvalidPhyMemory();
            long usedMem = MemoryHelper.WorkingSet64();
            if (IntPtr.Size == 8)
            {
                //if (avalidPhyMemory < avalidPhyMemoryMin * 1024 * 1024)
                //    throw new Exception(string.Format("当前系统资源不足以完成该操作，请释放部分资源后再试，剩余{0}<{1}，已使用{2}<{3}。",
                //        avalidPhyMemory / (1024f * 1024), avalidPhyMemoryMin, usedMem / (1024f * 1024), usedMemMax));
            }
            else
            {
                if (avalidPhyMemory < (ulong) (avalidPhyMemoryMin * 1024 * 1024) || usedMem > usedMemMax * 1024 * 1024)
                    throw new Exception(string.Format("当前系统资源不足以完成该操作，请释放部分资源后再试，剩余{0}<{1}，已使用{2}<{3}。",
                        avalidPhyMemory / (1024f * 1024), avalidPhyMemoryMin, usedMem / (1024f * 1024), usedMemMax));
            }
        }

        /// <summary>
        /// 当前可以申请的最大byte数组的大小
        /// </summary>
        /// <param name="arrayCount">byte数组个数</param>
        /// <returns></returns>
        public static ulong GetMaxArrayLength<T>(int arrayCount)
        {
            //by chennan 20140810 封装后的投影FY3C MERSI 250米投影为黑色，原因内存计算错误（不止托管内存可用）
            //ulong memLong = Process.GetCurrentProcess().PrivateMemorySize64;
            var memLong = GetAvalidPhyMemory();
            memLong = memLong / (uint) arrayCount;
            memLong = int.MaxValue / 2 > memLong ? memLong : int.MaxValue / 2;
            Debug.WriteLine(string.Format("Can Use Memory {0}*{1}byte,{2}MB", arrayCount, memLong,
                (uint) arrayCount * memLong / 1048576f));
            Console.WriteLine
            (string.Format("Can Use Memory {0}*{1}byte,{2}MB", arrayCount, memLong,
                (uint) arrayCount * memLong / 1048576f));
            return Convert.ToUInt64(memLong);
        }
    }
}