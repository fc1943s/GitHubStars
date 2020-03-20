let CONFIG = {
    indexHtmlTemplate: './public/index.html',
    fsharpEntry: './GitHubStars.Frontend.fsproj',
    outputDir: './dist',
    assetsDir: './public',
    devServerPort: 8087,
    devServerProxy: {
        '/api/*': {
            target: 'http://localhost:8086',
            changeOrigin: true
        },
    },
    babel: {
        presets: [
            ["@babel/preset-env", {
                "targets": {"node": "12"},
                "modules": false,
                "useBuiltIns": "usage",
                "corejs": 3,
            }],
            ["@babel/preset-react", {}]
        ],
    }
};

let isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

let path = require("path");
let webpack = require("webpack");
let HtmlWebpackPlugin = require('html-webpack-plugin');
let CopyWebpackPlugin = require('copy-webpack-plugin');
let MiniCssExtractPlugin = require("mini-css-extract-plugin");
let ReplaceInFileWebpackPlugin = require('replace-in-file-webpack-plugin');

let commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: CONFIG.indexHtmlTemplate
    })
];

module.exports = {
    entry: {
        app: [CONFIG.fsharpEntry]
    },
    output: {
        path: path.join(__dirname, CONFIG.outputDir),
        filename: isProduction ? '[name].[hash].js' : '[name].js',
        devtoolModuleFilenameTemplate: info => path.resolve(info.absoluteResourcePath).replace(/\\/g, '/'),
    },
    mode: isProduction ? "production" : "development",
    devtool: isProduction ? "nosources-source-map" : "eval-source-map",
    optimization: {
        splitChunks: {
            chunks: "all"
        }
    },
    plugins: 
        isProduction 
        ? commonPlugins.concat([
            new MiniCssExtractPlugin({ filename: 'style.css' }),
            new CopyWebpackPlugin([{ from: CONFIG.assetsDir }]),
            new ReplaceInFileWebpackPlugin([{
                test: [/\.js$/],
                rules: [{
                    search: /\|API_URL\|/g,
                    replace: process.env.API_URL || "http://localhost:8086"
                }]
            }])
        ]) 
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin()
        ]),
    resolve: {
        symlinks: false
    },
    devServer: {
        host: "0.0.0.0",
        publicPath: "/",
        contentBase: CONFIG.assetsDir,
        port: CONFIG.devServerPort,
        proxy: CONFIG.devServerProxy,
        hot: true,
        inline: true
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: CONFIG.babel
                    }
                }
            },
            {
                test: /\.[tj]sx?$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: CONFIG.babel
                },
            },
            {
                test: /\.(sass|scss|css)$/,
                use: [
                    isProduction ? MiniCssExtractPlugin.loader : 'style-loader',
                    'css-loader',
                    'sass-loader',
                ],
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ["file-loader"]
            }
        ]
    }
};

