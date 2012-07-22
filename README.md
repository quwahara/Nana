# Nana プログラミング言語

## はじめに

Nana プログラミング言語は .NET Framework で動くプログラミング言語です。
Nana は「左から右に代入したい！」を野望に開発を始めました。
そのころはまだ **R言語** の存在を知りませんでした。
ほかに目指しているところは
* なるべく一人でメンテナンスできるように、コンパイラの実装が単純であること
* なるべくタイプしないでいいように済むような文法であること

です。

完成度は低く、まだ簡単な文法しかビルドできません。
エラー処理なども、まだろくに実装できていません。
とりあえず少し動くようになりましたという感じです。

## 実行環境

Nana を使うには
* Windows XP 32bit
* Microsoft.NET Framework v2.0

が必要です。
多分 v2.0 以上の .NET が入っていれば、Widows Vsita や 7 でも動くと思います。

## 動かし方

とりあえず空の c:\tmp ディレクトリがあるとして、
そこに次の3つのファイルをダウンロードします。

https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/Nana.exe
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/NanaLib.dll
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/HelloWorld.nana

cmd.exe を開き、c:\tmp の下に移動し、次のように入力、リターンします。

```
C:\Tmp>Nana.exe HelloWorld.nana
```

うまくいくと次のようなメッセージが出ます。

```
Microsoft (R) .NET Framework IL Assembler.  Version 2.0.50727.3053
Copyright (c) Microsoft Corporation.  All rights reserved.
Assembling 'HelloWorld.il'  to EXE --> 'HelloWorld.exe'
Source file is ANSI

Assembled global method .cctor
Assembled global method 0
Creating PE file

Emitting classes:

Emitting fields and methods:
Global  Methods: 2;

Emitting events and properties:
Global
Writing PE file
Operation completed successfully



C:\Tmp>

```

そして同じディレクトリに  HelloWorld.exe ができています。
それを実行すると下のように出ます。

```
C:\Tmp>HelloWorld.exe
Hello, world!

```
おめでとうございます！
あなたの環境で立派に動きました。


うまくいかないときは、次のことを試してみて下さい。
Nanaでは ilasm.exe というプログラムを実行しています。
これは通常、下のディレクトリに配置されていてます。
Nana では規定でそこに ilasm.exe があることを期待しています。
```
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```


環境変数 NANA_ILASM_PATH で次のように直接 ilasm.exe の場所を指定できます。
お使いの環境で ilasm.exe があるところを確認して指定して下さい。

```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```
or
```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\ilasm.exe
```





## 実例
0 から 16 までのフィボナッチ数列を表示します
```
//  Save as 'fibo.nana'
//  build:
//  > nana fibo
fun Fibo(n:int):int
..
    if      0 == n then
        return 0
    elif    1 == n then
        return 1
    else
        return Fibo(n - 2) + Fibo(n - 1)
    end
,,

num = 0
while   17 > num do
    Fibo(num)   -> fi
    `p(fi)
    num = num + 1
end
```

## 文法

### 整数リテラル

```
i1  = 2147483647                //  int型になります。int.MaxValue以下に収まっているので。
i2  = 2147483648                //  uint型になります。int.MaxValueを超えているので。
i3  = 4294967295                //  uint型になります。uint.MaxValuee以下に収まっているので。
i4  = 4294967296                //  longt型になります。uint.MaxValueを超えているので。
i5  = 9223372036854775807       //  long型になります。long.MaxValue以下に収まっているので。
i6  = 9223372036854775808       //  ulong型になります。long.MaxValueを超えているので。

im1 = -2147483648               //  int型になります。int.MinValue以上に収まっているので。
im2 = -2147483649               //  longt型になります。int.MinValueを下回っているので。
im3 = -9223372036854775808      //  longt型になります。

ul  = 1UL                       //  ulong型になります。

l1  = 9223372036854775807L      //  long型になります。long.MaxValue以下に収まっているので。
l2  = 9223372036854775808L      //  ulong型になります。long.MaxValueを超えているので。

u1  = 4294967295u               //  uint型になります。uint.MaxValue以下に収まっているので
u2  = 4294967296u               //  ulong型になります。uint.MaxValueを超えているので。

//  #16進数による整数リテラル(よく'0x'で始まる)記法にはまだ*対応していません。*
```

### 実数リテラル

```
d1  = 0.1                       //  小数を指定すると double型になります
f1  = 0.1F                      //  suffix 'F' を指定すると float型になります
d2  = 314E-2                    //  'E' 10の階乗の指数を指定できます。double型になります。
```

### 参照型キャスト

```
//  "as" の後に指定されている型にキャストします。
//  キャストに失敗したときは null になります。

o   = "hi" as object    // => o は string型 "hi" を持つ object 型の変数になる

p   = 1 as object       // => p は null を持つ object 型の変数になる


//  "as!" の後に指定されている型にキャストします。
//  キャストに失敗したときは System.InvalidCastException が投入されます。

q   = "hi" as! object   // => q は string型 "hi" を持つ object 型の変数になる

r   = 1 as! string      // => System.InvalidCastException が投入される
```

## ライセンス

MIT ライセンスで公開しています。 
ライセンスの全文は下で参照できます。

https://raw.github.com/quwahara/Nana/master/LICENSE

