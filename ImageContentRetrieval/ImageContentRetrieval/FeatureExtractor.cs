/*
 * 本代码适用于
 * TensorFlow.NET 0.40.1
 * SciSharp.TensorFlow.Redist 2.5.0
 * 
 * 
 */

using NumSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tensorflow;
using static Tensorflow.Binding;

namespace ImageContentRetrieval
{
    internal static class FeatureExtractor
    {
        public static string GetExecutionDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }


        public static string GetFileAbsolutePath(string filename)
        {
            return Path.Combine(GetExecutionDirectory(), filename);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static (Graph graph, Tensor bottleneck, Tensor image) import_graph()
        {
            var graph = tf.Graph().as_default();

            var bytes = File.ReadAllBytes(GetFileAbsolutePath("classify_image_graph_def.pb"));
            var graphDef = GraphDef.Parser.ParseFrom(bytes);

            //foreach (var n in graphDef.Node)
            //{
            //    Debug.WriteLine(n);
            //}

            var return_tensors = tf.import_graph_def(
                graphDef,
                name: "",
                return_elements: new[]
                {
                "pool_3/_reshape:0",
                "DecodeJpeg/contents:0"
                })
                .Select(x => x as Tensor)
                .ToArray();


            var bottleneck = return_tensors[0];
            var image = return_tensors[1];

            return (graph, bottleneck, image);
        }




        private static NDArray get_bottleneck_data(Session session, Tensor bottleneck, Tensor image, string image_file)
        {
            try
            {
                var image_data = File.ReadAllBytes(image_file);
                var bottleneck_data = session.run(bottleneck, (image, new Tensor(image_data, TF_DataType.TF_STRING)));
                bottleneck_data = np.squeeze(bottleneck_data);
                return bottleneck_data;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static async Task<IEnumerable<(string, float[])>> GetImagesFeatures(IEnumerable<string> imageFilenames)
        {
            return await Task.Run(() =>
            {

                tf.compat.v1.disable_eager_execution();

                var (graph, bottleneck, image) = import_graph();

                var config = new ConfigProto
                {
                    AllowSoftPlacement = true,
                    GpuOptions = new GPUOptions
                    {
                        AllowGrowth = true,
                        ForceGpuCompatible = true
                    }
                };

                //config.DeviceCount.Add("GPU", 1);
                //config.DeviceCount.Add("CPU", 0);

                var features = new List<(string, float[])>();

                using var session = new Session(graph, config);

                foreach (var img in imageFilenames)
                {
                    var feature = get_bottleneck_data(session, bottleneck, image, img);

                    //Console.WriteLine(feature.ToString());
                    if (feature == null)
                        continue;

                    features.Add((img, feature.ToArray<float>()));
                }

                return features;

            });
        }

        public static float[] GetImageFeature(string imageFilename)
        {
            //tf.compat.v1.disable_eager_execution();

            var (graph, bottleneck, image) = import_graph();

            var config = new ConfigProto
            {
                AllowSoftPlacement = true,
                GpuOptions = new GPUOptions
                {
                    AllowGrowth = true,
                    ForceGpuCompatible = true
                }
            };

            using var session = new Session(graph, config);

            var feature = get_bottleneck_data(session, bottleneck, image, imageFilename);



            return feature.ToArray<float>();

        }

    }
}
