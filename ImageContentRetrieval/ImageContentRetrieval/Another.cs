//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using Tensorflow;
//using Tensorflow.NumPy;
//using static Tensorflow.Binding;


//namespace ImageContentRetrieval
//{
//    public class Another
//    {
//        public static string GetExecutionDirectory()
//        {
//            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
//        }


//        public static string GetFileAbsolutePath(string filename)
//        {
//            return Path.Combine(GetExecutionDirectory(), filename);
//        }


//        //private static NDArray ReadTensorFromImageFile(Session session, string file_name,
//        //           int input_height = 224,
//        //           int input_width = 224,
//        //           int input_mean = 117,
//        //           int input_std = 1)
//        //{
//        //    var graph = tf.Graph().as_default();

//        //    var file_reader = tf.io.read_file(file_name, "file_reader");
//        //    var decodeJpeg = tf.image.decode_jpeg(file_reader, channels: 3, name: "DecodeJpeg");
//        //    var cast = tf.cast(decodeJpeg, tf.float32);
//        //    var dims_expander = tf.expand_dims(cast, 0);
//        //    var resize = tf.constant(new int[] { input_height, input_width });
//        //    var bilinear = tf.image.resize_bilinear(dims_expander, resize);
//        //    var sub = tf.subtract(bilinear, new float[] { input_mean });
//        //    var normalized = tf.divide(sub, new float[] { input_std });

//        //    using (var sess = tf.Session(graph))
//        //        return sess.run(normalized);
//        //}



//        public static NDArray ReadTensorFromImageFile(Session session, string file_name)
//        {
//            var file_reader = tf.io.read_file(file_name, "file_reader");
//            var decodeJpeg = tf.image.decode_image(file_reader, channels: 3, name: "DecodeImage");

//            return session.run(decodeJpeg);
//        }



//        public static (Graph graph, Tensor bottleneck, Tensor expandDims_dim) import_graph()
//        {
//            var graph = tf.Graph().as_default();

//            var bytes = File.ReadAllBytes(GetFileAbsolutePath("classify_image_graph_def.pb"));
//            var graphDef = GraphDef.Parser.ParseFrom(bytes);

//            foreach (var n in graphDef.Node)
//            {
//                Debug.WriteLine(n.Name);
//            }

//            var return_tensors = tf.import_graph_def(
//                graphDef,
//                name: "",
//                return_elements: new[]
//                {
//                    "pool_3/_reshape:0",
//                    "ExpandDims/dim:0"
//                })
//                .Select(x => x as Tensor)
//                .ToArray();


//            var bottleneck = return_tensors[0];
//            var expandDims_dim = return_tensors[1];

//            return (graph, bottleneck, expandDims_dim);
//        }


//        public static NDArray get_bottleneck_data(Session session, Tensor bottleneck, Tensor expandDims_dim, string image_file)
//        {
//            //try
//            //{

//            var file_reader = tf.io.read_file(image_file, "file_reader");
//            var decodeImage = tf.image.decode_image(file_reader, channels: 3, name: "DecodeImage");
//            var cast = tf.cast(decodeImage, tf.int32);

       

//            var bottleneck_data = session.run(bottleneck, (expandDims_dim, cast));
//            bottleneck_data = np.squeeze(bottleneck_data);
//            return bottleneck_data;
//            //}
//            //catch (Exception)
//            //{
//            //    return null;
//            //}
//        }


//    }
//}
