using System;
using System.Text;
using System.Windows;

namespace DES
{
    public partial class MainWindow : Window
    {
        const int Block_size = 64;
        const int Key_size = 56;
        const int Key_i_size = 48;
        private readonly int[] G_table = new int[Key_size]
        {
            57, 49, 41, 33, 25, 17, 09,
            01, 58, 50, 42, 34, 26, 18,
            10, 02, 59, 51, 43, 35, 27,
            19, 11, 03, 60, 52, 44, 36,
            63, 55, 47, 39, 31, 23, 15,
            07, 62, 54, 46, 38, 30, 22,
            14, 06, 61, 53, 45, 37, 29,
            21, 13, 05, 28, 20, 12, 04
        };
        private readonly int[] P_table = new int[Block_size / 2]
        {
            16, 7, 20, 21, 29, 12, 28, 17,
            1, 15, 23, 26, 5, 18, 31, 10,
            2, 8, 24, 14, 32, 27, 3, 9,
            19, 13, 30, 6, 22, 11, 4, 25
        };
        private readonly int[] IP_table = new int[Block_size]
        {
            58, 50, 42, 34, 26, 18, 10, 2,
            60, 52, 44, 36, 28, 20, 12, 4,
            62, 54, 46, 38, 30, 22, 14, 6,
            64, 56, 48, 40, 32, 24, 16, 8,
            57, 49, 41, 33, 25, 17, 9, 1,
            59, 51, 43, 35, 27, 19, 11, 3,
            61, 53, 45, 37, 29, 21, 13, 5,
            63, 55, 47, 39, 31, 23, 15, 7
        };
        private readonly int[] E_table = new int[Key_i_size]
        {
            32, 01, 02, 03, 04, 05,
            04, 05, 06, 07, 08, 09,
            08, 09, 10, 11, 12, 13,
            12, 13, 14, 15, 16, 17,
            16, 17, 18, 19, 20, 21,
            20, 21, 22, 23, 24, 25,
            24, 25, 26, 27, 28, 29,
            28, 29, 30, 31, 32, 01
        };
        private readonly int[] H_table = new int[Key_i_size]
        {
            14, 17, 11, 24, 01, 05, 03, 28,
            15, 06, 21, 10, 23, 19, 12, 04,
            26, 08, 16, 07, 27, 20, 13, 02,
            41, 52, 31, 37, 47, 55, 30, 40,
            51, 45, 33, 48, 44, 49, 39, 56,
            34, 53, 46, 42, 50, 36, 29, 32
        };
        private readonly int[] IP_1_table = new int[Block_size]
        {
            40, 08, 48, 16, 56, 24, 64, 32,
            39, 07, 47, 15, 55, 23, 63, 31,
            38, 06, 46, 14, 54, 22, 62, 30,
            37, 05, 45, 13, 53, 21, 61, 29,
            36, 04, 44, 12, 52, 20, 60, 28,
            35, 03, 43, 11, 51, 19, 59, 27,
            34, 02, 42, 10, 50, 18, 58, 26,
            33, 01, 41, 09, 49, 17, 57, 25
        };
        private class NotBitArray
        {
            public string bits;
            public override string ToString() => bits;
            public NotBitArray()
            {
                bits = "";
            }
            public NotBitArray(int N)
            {
                bits = new string('0', N);
            }
            public NotBitArray(byte[] bytes)
            {
                StringBuilder sb = new StringBuilder(bytes.Length * 8);
                foreach (byte b in bytes)
                    sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));

                bits = sb.ToString();
            }
            public NotBitArray(string s)
            {
                if (string.IsNullOrWhiteSpace(s.Replace("0", "").Replace("1", "")))
                    bits = s;
                else
                    bits = "";
            }
            public bool this[int i]
            {
                get { return bits[i] == '1'; }
                set
                {
                    char[] char_bits = bits.ToCharArray();
                    char_bits[i] = value ? '1' : '0';
                    bits = new string(char_bits);
                }
            }
            public byte[] ToByteArray()
            {
                int length = bits.Length / 8;
                byte[] bytes = new byte[length];
                for (int i = 0; i < length; i++)
                    bytes[i] = Convert.ToByte(bits.Substring(i * 8, 8), 2);
                return bytes;
            }
            public NotBitArray Xor(NotBitArray block)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bits.Length; i++)
                    if (this[i] == block[i])
                        sb.Append('0');
                    else
                        sb.Append('1');
                return new NotBitArray(sb.ToString());
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        byte[] DES(byte[] byte_block, byte[] byte_key, int round_count, bool is_encoding)
        {
            NotBitArray key = new NotBitArray(byte_key);
            NotBitArray block = new NotBitArray(byte_block);

            NotBitArray CD_i = Permutation(key, G_table); // C_0 + D_0

            NotBitArray[] Keys = new NotBitArray[round_count];
            for (int i = 0; i < round_count; i++)
            {
                CD_i = Get_CD_i(i, CD_i);
                Keys[i] = Permutation(CD_i, H_table);
            }
            NotBitArray Output = Coding_rounds(round_count, block, Keys, is_encoding);
            return Output.ToByteArray();
        }
        private NotBitArray Coding_rounds(int round_count, NotBitArray block, NotBitArray[] Keys, bool is_encoding)
        {
            int start, finish, step;
            if (is_encoding)
            {
                start = 1;
                step = 1;
                finish = round_count + 1;
            }
            else
            {
                start = round_count;
                step = -1;
                finish = 0;
            }
            block = Permutation(block, IP_table);
            NotBitArray L_block = new NotBitArray(block.bits.Substring(0, Block_size / 2)),
                        R_block = new NotBitArray(block.bits.Substring(Block_size / 2));

            for (int i = start; i != finish; i += step)
            {
                NotBitArray buffer = R_block;

                R_block = L_block.Xor(F(R_block, Keys[i - 1])); 
                L_block = buffer;
            }
            return Permutation(new NotBitArray(R_block.bits + L_block.bits), IP_1_table);
        }
        NotBitArray F(NotBitArray B, NotBitArray K)
        {
            NotBitArray E_B = Permutation(B, E_table); // Extending
            E_B = E_B.Xor(K);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                NotBitArray B_i = new NotBitArray(E_B.bits.Substring(i * 6, 6));    // Get 6bit vector
                NotBitArray S_i = S(i, B_i);                         // Change to 4bit
                sb.Append(S_i.bits);                                 // Add to result
            }
            return Permutation(new NotBitArray(sb.ToString()), P_table); // Final premutation for F(R_i, K_i)
        }
        NotBitArray Get_CD_i(int round, NotBitArray CD)
        {
            int shift = round > 1 &&                    //  instead of shifts table; 
                        (round + 1) % 9 != 0 &&         //  '%' to have some 1-bit shift 
                        (round + 1) % 16 != 0 ? 2 : 1;  //  on more then 16 rounds

            return new NotBitArray(new StringBuilder()
                    .Append(CD.bits.Substring(shift, 28 - shift))
                    .Append(CD.bits.Substring(0, shift))
                    .Append(CD.bits.Substring(28 + shift, 28 - shift))
                    .Append(CD.bits.Substring(28, shift)).ToString());
        }   // Shifts C_i, D_i vectors
        NotBitArray S(int i, NotBitArray B_i)
        {
            int a = 0, b = 0;

            a += B_i[0] ? 2 : 0;
            a += B_i[5] ? 1 : 0;

            for (int b_count = 0; b_count < 4; b_count++)
                b += B_i[4 - b_count] ? 1 << b_count : 0;

            int[][] S_table;
            switch (i)
            {
                case 0:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] { 14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7},
                        new int[16] { 0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8},
                        new int[16] { 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0},
                        new int[16] { 15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13}
                        };
                        break;
                    }
                case 1:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] {15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10},
                        new int[16] {3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5},
                        new int[16] {0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15},
                        new int[16] {13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9}
                        };
                        break;
                    }
                case 2:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] {10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8},
                        new int[16] {13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1},
                        new int[16] {13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7},
                        new int[16] {1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12}
                        };
                        break;
                    }
                case 3:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] { 7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15 },
                        new int[16] { 13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9 },
                        new int[16] { 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4 },
                        new int[16] { 3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14 }
                        };
                        break;
                    }
                case 4:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] { 2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9 },
                        new int[16] { 14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6 },
                        new int[16] { 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14 },
                        new int[16] { 11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3 }
                        };
                        break;
                    }
                case 5:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] { 12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11 },
                        new int[16] { 10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8 },
                        new int[16] { 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6 },
                        new int[16] { 4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13 }
                        };
                        break;
                    }
                case 6:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] { 4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1 },
                        new int[16] { 13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6 },
                        new int[16] { 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2 },
                        new int[16] { 6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12 }
                        };
                        break;
                    }
                case 7:
                    {
                        S_table = new int[4][]
                        {
                        new int[16] {13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7},
                        new int[16] {1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2},
                        new int[16] {7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8},
                        new int[16] {2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11}
                        };
                        break;
                    }
                default: return null;
            };  // Get S table for i's B

            int B_out = S_table[a][b];

            StringBuilder sb = new StringBuilder();
            for (int c = 0; c < 4; c++)
                sb.Append((B_out & (1 << c)) != 0 ? '1' : '0');

            return new NotBitArray(sb.ToString());
        }
        NotBitArray Permutation(NotBitArray Input_array, int[] table)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < table.Length; i++)
                sb.Append(Input_array.bits[table[i] - 1]);
            return new NotBitArray(sb.ToString());
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string Text = Text_textbox.Text;
            string Password = Key_textbox.Text;
            bool is_encoding = Decoding_radiobutton.IsChecked == false;

            if (string.IsNullOrEmpty(Text) ||
                string.IsNullOrEmpty(Password) ||
                !int.TryParse(Rounds_textbox.Text, out int rounds) ||
                rounds < 1)
            {
                MessageBox.Show("Write text, password and quantity of rounds");
                return;
            }
            byte[] byte_text, byte_key;

            if (As_HEX_radiobutton.IsChecked == true)
            {
                Password = Password.Replace("-", "");
                Text = Text.Replace("-", "");

                if (Password.Length != 16)
                {
                    MessageBox.Show("Password should have 8 byte");
                    return;
                }
                if ((byte_text = Get_Bytes_From_HEX(Text)) == null ||
                    (byte_key = Get_Bytes_From_HEX(Password)) == null)
                {
                    MessageBox.Show("Wrong HEX code");
                    return;
                }
            }
            else
            {
                if (Password.Length != 4)
                {
                    MessageBox.Show("Password should have 8 byte; text should be multiplie of 8 bytes");
                    return;
                }
                byte_text = Encoding.Unicode.GetBytes(Text);
                byte_key = Encoding.Unicode.GetBytes(Password);
            }
            if (byte_text.Length % 8 != 0)
            {
                byte[] more_bytes = new byte[(byte_text.Length / 8 + 1) * 8];
                byte_text.CopyTo(more_bytes, 0);
                for (int i = byte_text.Length; i < more_bytes.Length; i++)
                    if (i == byte_text.Length)
                        more_bytes[i] = 64;
                    else
                        more_bytes[i] = 0;
                byte_text = more_bytes;
            }
            byte[] output_bytes = new byte[byte_text.Length];
            for (int i = 0; i < byte_text.Length / 8; i++)
            {
                byte[] block = new byte[8];
                Array.Copy(byte_text, i * 8, block, 0, 8);
                DES(block, byte_key, rounds, is_encoding).CopyTo(output_bytes, i * 8);
            }
            string output_text;
            if (As_HEX_radiobutton.IsChecked == true)
                output_text = BitConverter.ToString(output_bytes);
            else
                output_text = Encoding.Unicode.GetString(output_bytes);

            Output_textbox.Text = output_text;
        }
        byte[] Get_Bytes_From_HEX(string text)
        {
            if (text.Length % 2 != 0)
                return null;
            byte[] byte_text = new byte[text.Length / 2];
            for (int i = 0; i < text.Length; i += 2)
                if (!byte.TryParse(text.Substring(i, 2),
                    System.Globalization.NumberStyles.HexNumber, null, out byte_text[i / 2]))
                    return null;

            return byte_text;
        }
        private void As_string_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            if (Output_textbox == null)
                return;
            if (!string.IsNullOrEmpty(Output_textbox.Text))
                Output_textbox.Text = Encoding.Unicode.GetString(Get_Bytes_From_HEX(Output_textbox.Text.Replace("-", "")));
            if (!string.IsNullOrEmpty(Key_textbox.Text))
            {
                byte[] bytes = Get_Bytes_From_HEX(Key_textbox.Text.Replace("-", ""));
                if (bytes != null)
                    Key_textbox.Text = Encoding.Unicode.GetString(bytes);
            }
            if (!string.IsNullOrEmpty(Text_textbox.Text))
            {
                byte[] bytes = Get_Bytes_From_HEX(Text_textbox.Text.Replace("-", ""));
                if (bytes != null)
                    Text_textbox.Text = Encoding.Unicode.GetString(Get_Bytes_From_HEX(Text_textbox.Text.Replace("-", "")));
            }
        }
        private void As_HEX_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            if (Output_textbox == null)
                return;
            if (!string.IsNullOrEmpty(Output_textbox.Text))
                Output_textbox.Text = BitConverter.ToString(Encoding.Unicode.GetBytes(Output_textbox.Text));
            if (!string.IsNullOrEmpty(Key_textbox.Text))
                Key_textbox.Text = BitConverter.ToString(Encoding.Unicode.GetBytes(Key_textbox.Text));
            if (!string.IsNullOrEmpty(Text_textbox.Text))
                Text_textbox.Text = BitConverter.ToString(Encoding.Unicode.GetBytes(Text_textbox.Text));
        }
    }
}
