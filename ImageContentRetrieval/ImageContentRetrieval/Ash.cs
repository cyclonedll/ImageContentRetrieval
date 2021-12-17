using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageContentRetrieval
{

    /*
     * ‎2021‎年‎7‎月‎17‎日，‏‎13:40:05 cyclone_dll 于昆明创建
     * 解决特征存储和检索问题
     *
     *
     * 结构如下：
     *  字符串长度， 字符串值（字节）,    特征序列（数组）
     *    int32       byte[] UTF-16编码     single[]    
     *
     */

    public class Ash
    {


        private List<(string, float[])> _features;

        private int _featureSize;

        private string _currentAshFile;


        public Ash(int featureSize)
        {
            _featureSize = featureSize;
            _features = new();
        }


        private Ash()
        {
            _features = new();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="features"></param>
        /// <param name="ashFilename"></param>
        /// <param name="featureSize"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void Build(IEnumerable<(string, float[])> features, string ashFilename, int featureSize)
        {
            if (features == null)
                throw new ArgumentNullException(nameof(features));

            if (ashFilename == null)
                throw new ArgumentNullException(nameof(ashFilename));

            using var fs = new FileStream(ashFilename, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            bw.Write(featureSize);

            foreach (var f in features)
            {
                if (f.Item2.Length != featureSize)
                    throw new ArgumentException($"Some feature size of item is not {featureSize}");

                /*1 先写文件名*/
                //不能直接使用bw.Write(string)
                //因为当文件名是中文时就会出错
                //路径有中文不影响
                //或许是个BUG
                var buffer = Encoding.Unicode.GetBytes(f.Item1);

                bw.Write(buffer.Length);
                bw.Write(buffer);


                /*2 再写特征组*/
                foreach (var item in f.Item2)
                {
                    bw.Write(item);
                }
            }
        }

        /// <summary>
        /// Build Ash file with specified filename.
        /// </summary>
        /// <param name="ashFilename"></param>
        public void BuildTo(string ashFilename)
            => Build(_features, ashFilename, _featureSize);


        /// <summary>
        /// Build Ash file with current loaded filename.
        /// </summary>
        public void Build()
            => Build(_features, _currentAshFile, _featureSize);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="feature"></param>
        /// <param name="checkExists"></param>
        /// <returns>若<paramref name="checkExists"/>为 false 则总是返回 true。否则添加成功返回true，存在则返回 false.</returns>
        public bool Add(string filename, float[] feature, bool checkExists = true)
        {
            if (feature.Length != _featureSize)
                throw new ArgumentException($"The size of feature is not {_featureSize}");

            if (checkExists)
            {
                if (_features.Exists((item) =>
                {
                    return item.Item1 == filename;
                }))
                    return false;
            }

            _features.Add((filename, feature));
            return true;
        }

        public bool Add((string, float[]) pack, bool checkExists = true)
        {
            if (pack.Item2.Length != _featureSize)
                throw new ArgumentException($"The size of feature is not {_featureSize}");

            if (checkExists)
            {
                if (_features.Exists((item) =>
                {
                    return item.Item1 == pack.Item1;
                }))
                    return false;
            }

            _features.Add(pack);
            return true;
        }


        /// <summary>
        ///  从当前特列表中排除与<paramref name="filenames"/>中相同的项，并返回新的序列.
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns></returns>
        public IEnumerable<string> Except(IEnumerable<string> filenames)
        {
            foreach (var fn in filenames)
            {
                //若不存在则迭代返回
                if (!_features.Exists(item =>
                {
                    return item.Item1 == fn;
                }))
                    yield return fn;
            }
        }



        public void RemoveAt(int index) => _features.RemoveAt(index);

        /// <summary>
        ///  移除所有文件名为 <paramref name="filename"/> 的特征组。
        /// </summary>
        /// <param name="filename"></param>
        public void Remove(string filename)
        {
            int index = 0;
            foreach (var feature in _features)
            {
                if (feature.Item1 == filename)
                    _features.RemoveAt(index);

                index++;
            }
        }

        /// <summary>
        /// Removes all items by condition.
        /// </summary>
        /// <param name="func"></param>
        public void Remove(Func<string, bool> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            int index = 0;
            foreach (var feature in _features)
            {
                if (func(feature.Item1))
                    _features.RemoveAt(index);

                index++;
            }
        }

        /// <summary>
        /// Removes all items from Current Ash Features.
        /// </summary>
        public void Clear() => _features.Clear();

        /// <summary>
        /// 
        /// </summary>
        public int Count => _features.Count;


        public int FeatureSize => _featureSize;


        public string CurrentAshFile => _currentAshFile;



        ///// <summary>
        ///// Load the ash file to Current Ash instance.
        ///// </summary>
        ///// <param name="ashFilename">The ash file to load.</param>
        ///// <exception cref="FileNotFoundException"></exception>
        //public void Load(string ashFilename)
        //{
        //    if (!File.Exists(ashFilename))
        //        throw new FileNotFoundException(ashFilename);

        //    _currentAshFile = ashFilename;
        //    _features.Clear();

        //    using var fs = new FileStream(ashFilename, FileMode.Open);
        //    using var br = new BinaryReader(fs);

        //    _featureSize = br.ReadInt32();

        //    while (fs.Position < fs.Length)
        //    {
        //        /*1 先读文件名*/
        //        var bufferLen = br.ReadInt32();
        //        var buffer = br.ReadBytes(bufferLen);
        //        var filename = Encoding.Unicode.GetString(buffer);

        //        /*2 再读特征组*/
        //        float[] feature = new float[_featureSize];
        //        for (int i = 0; i < _featureSize; i++)
        //        {
        //            feature[i] = br.ReadSingle();
        //        }

        //        _features.Add((filename, feature));

        //    }
        //}


        /// <summary>
        /// Load a Ash file ans returns <see cref="Ash"/> instance.
        /// </summary>
        /// <param name="ashFilename"></param>
        /// <returns></returns>
        public static Ash Load(string ashFilename)
        {
            var result = new Ash
            {
                _currentAshFile = ashFilename
            };

            using var fs = new FileStream(ashFilename, FileMode.Open);
            using var br = new BinaryReader(fs);

            result._featureSize = br.ReadInt32();

            while (fs.Position < fs.Length)
            {
                /*1 先读文件名*/
                var bufferLen = br.ReadInt32();
                var buffer = br.ReadBytes(bufferLen);
                var filename = Encoding.Unicode.GetString(buffer);

                /*2 再读特征组*/
                float[] feature = new float[result._featureSize];
                for (int i = 0; i < result._featureSize; i++)
                {
                    feature[i] = br.ReadSingle();
                }

                result._features.Add((filename, feature));
            }

            return result;
        }


        /// <summary>
        /// Factory method : To load a Ash file asynchously.
        /// </summary>
        /// <param name="ashFilename">The Ash File to load.</param>
        /// <returns></returns>
        public static async Task<Ash> LoadAsync(string ashFilename)
        {
            if (!File.Exists(ashFilename))
                throw new FileNotFoundException(ashFilename);

            return await Task.Run(() =>
            {
                var result = new Ash
                {
                    _currentAshFile = ashFilename
                };

                using var fs = new FileStream(ashFilename, FileMode.Open);
                using var br = new BinaryReader(fs);

                result._featureSize = br.ReadInt32();

                while (fs.Position < fs.Length)
                {
                    /*1 先读文件名*/
                    var bufferLen = br.ReadInt32();
                    var buffer = br.ReadBytes(bufferLen);
                    var filename = Encoding.Unicode.GetString(buffer);

                    /*2 再读特征组*/
                    float[] feature = new float[result._featureSize];
                    for (int i = 0; i < result._featureSize; i++)
                    {
                        feature[i] = br.ReadSingle();
                    }

                    result._features.Add((filename, feature));
                }

                return result;

            });
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="features"></param>
        /// <param name="topItems"></param>
        /// <returns>(distance,index,filename)</returns>
        public static IEnumerable<(float, int, string)> Retrieval
            (float[] target, IEnumerable<(string, float[])> features, int topItems)
        {

            var distances = new List<(float, int, string)>();

            int index = 0;
            foreach (var feature in features)
            {
                var distance = Euclidean.Distance(target, feature.Item2);
                distances.Add((distance, index, feature.Item1));
                index++;
            }

            var actualTopItems = Math.Min(distances.Count, topItems);

            distances.Sort();

            for (int i = 0; i < actualTopItems; i++)
            {
                yield return distances[i];
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="topItems"></param>
        /// <returns>(distance,index,filename)</returns>
        public IEnumerable<(float, int, string)> Retrieval(float[] target, int topItems)
        {
            return Retrieval(target, _features, topItems);
        }



        public static void Cleanup(string ashFilename)
        {
            if (!File.Exists(ashFilename))
                throw new FileNotFoundException(ashFilename);

            var features = new List<(string, float[])>();

            var fs = new FileStream(ashFilename, FileMode.Open);
            var br = new BinaryReader(fs);

            var featureSize = br.ReadInt32();

            while (fs.Position < fs.Length)
            {

                /*1 先写文件名*/
                var bufferLen = br.ReadInt32();
                var buffer = br.ReadBytes(bufferLen);
                var filename = Encoding.Unicode.GetString(buffer);

                /*2 再写特征组*/
                float[] feature = new float[featureSize];
                for (int i = 0; i < featureSize; i++)
                {
                    feature[i] = br.ReadSingle();
                }


                features.Add((filename, feature));
            }


            var distinct = features.Distinct(new FeaturePackComparer());

            var finnalFeatures = new List<(string, float[])>();
            foreach (var f in distinct)
            {
                if (File.Exists(f.Item1))
                    finnalFeatures.Add(f);
            }

            br.Dispose();
            fs.Dispose();

            //之前先读出所有项，排除重复项
            //接下来再写入文件
            Build(finnalFeatures, ashFilename, featureSize);

        }

        public static async Task CleanupAsync(string ashFile)
        {
            await Task.Run(() =>
            {
                Cleanup(ashFile);
            });
        }



        public void Cleanup() => Cleanup(_currentAshFile);


        public async Task CleanupAsync()
        {
            await Task.Run(() =>
            {
                Cleanup(_currentAshFile);
            });
        }


        public class FeaturePackComparer : IEqualityComparer<(string, float[])>
        {
            public bool Equals((string, float[]) x, (string, float[]) y)
            {
                return x.Item1 == y.Item1;
            }

            public int GetHashCode([DisallowNull] (string, float[]) obj)
            {
                return obj.Item1.GetHashCode();
            }
        }
    }
}
