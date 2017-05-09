using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace StardewValleyMP
{
    class Util
    {
        public static bool UsingMono
        {
            get { return Type.GetType("Mono.Runtime") != null; }
        }

        public static Texture2D WHITE_1X1;

        public static string getLocalIp()
        {
            try
            {
                // http://stackoverflow.com/a/27376368
                using (Socket socket = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    return (socket.LocalEndPoint as IPEndPoint).Address.ToString();
                }
            }
            catch (Exception e)
            {
                Log.warn("Exception getting internal IP: " + e);
                return null;
            }
        }
        public static string getExternalIp()
        {
            try
            {
                return new WebClient().DownloadString("http://ipinfo.io/ip").Trim();
            }
            catch (Exception e)
            {
                Log.warn("Exception getting external IP: " + e);
                return null;
            }
        }

        public static void drawStr(string str, float x, float y, Color col, float alpha = 1, bool smallFont = true)
        {
            for ( int i = 0; i < str.Length; ++i )
            {
                if ( !Game1.smallFont.Characters.Contains( str[ i ] ) )
                {
                    str = str.Remove(i, 1).Insert(i, "?");
                } 
            }
            /*SpriteBatch b = Game1.spriteBatch;
            
            b.DrawString(Game1.smallFont, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 2f), Game1.textShadowColor * alpha);
            b.DrawString(Game1.smallFont, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), Game1.textShadowColor * alpha);
            b.DrawString(Game1.smallFont, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), Game1.textShadowColor * alpha);
            b.DrawString(Game1.smallFont, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), col * 0.9f * alpha);*/
            SpriteBatch b = Game1.spriteBatch;
            Color inverted = new Color(255 - col.R, 255 - col.G, 255 - col.B);

            SpriteFont font = smallFont ? Game1.smallFont : Game1.dialogueFont;
            b.DrawString(font, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(-2f, 0f), inverted * alpha * 0.8f);
            b.DrawString(font, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(2f, 0f), inverted * alpha * 0.8f);
            b.DrawString(font, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, 2f), inverted * alpha * 0.8f);
            b.DrawString(font, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)) + new Vector2(0f, -2f), inverted * alpha * 0.8f);
            b.DrawString(font, str, new Vector2((float)(x + Game1.tileSize / 4), (float)(y + Game1.tileSize / 4 + 4)), col * 0.9f * alpha);
        }

        // http://stackoverflow.com/a/22456034
        public static string serialize< T >( T obj )
        {
            using ( MemoryStream stream = new MemoryStream() )
            {
                XmlSerializer serializer = new XmlSerializer( obj.GetType() );
                serializer.Serialize(stream, obj);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T deserialize< T >( string str )
        {
            int beg = str.IndexOf('<');
            //string root = str.Substring(beg + 1, str.IndexOf(" xmlns") - beg - 1);
            XmlSerializer serializer = new XmlSerializer(typeof(T)/*, new XmlRootAttribute( root )*/);

            using ( TextReader reader = new StringReader( str ) )
            {
                return ( T )serializer.Deserialize(reader);
            }
        }

        // http://stackoverflow.com/questions/1879395/how-to-generate-a-stream-from-a-string
        public static Stream stringStream( string str )
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        // http://stackoverflow.com/questions/3303126/how-to-get-the-value-of-private-field-in-c
        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        public static void SetInstanceField(Type type, object instance, string fieldName, object value)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            field.SetValue(instance, value);
        }

        public static object GetStaticField(Type type, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(null);
        }

        public static void SetStaticField(Type type, string fieldName, object value)
        {
            BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            field.SetValue(null, value);
        }

        public static void CallStaticMethod(Type type, string name, object[] args)
        {
            // TODO: Support method overloading
            BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            MethodInfo func = type.GetMethod(name, bindFlags);
            func.Invoke(null, args);
        }

        public static void CallInstanceMethod(Type type, object instance, string name, object[] args)
        {
            // TODO: Support method overloading
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            MethodInfo func = type.GetMethod(name, bindFlags);
            func.Invoke(instance, args);
        }

        public static bool AreEqual< TKey, TVal >( Dictionary< TKey, TVal > a, Dictionary< TKey, TVal > b,
                                                   Func< TVal, TVal, bool > comp = null )
        {
            if (a.Count != b.Count)
                return false;
            else
            {
                List<TKey> keys = a.Keys.ToList();
                foreach (TKey key in b.Keys)
                {
                    if (!keys.Contains(key))
                    {
                        // A key is in the new but not old.
                        return false;
                    }
                    else if ( ( comp == null && !a[ key ].Equals( b[ key ] ) ) || ( comp != null && !comp( a[ key ], b[ key ] ) ) )
                    {
                        return false;
                    }
                    else
                    {
                        keys.Remove(key);
                    }
                }

                // A key was in the old but not the new.
                if (keys.Count > 0)
                    return false;
            }

            return true;
        }

        // Compression utils:
        /// <summary>
        /// Compresses byte array to new byte array.
        /// </summary>
        public static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionLevel.Optimal))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        public static byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
